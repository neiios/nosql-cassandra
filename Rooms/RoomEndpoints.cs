using Cassandra;
using ISession = Cassandra.ISession;

namespace CassandraChat.Rooms;

public static class RoomEndpoints
{
    public static RouteGroupBuilder MapRoomsApi(this RouteGroupBuilder app, ISession session)
    {
        app.MapPost("/", async (RoomCreateRequestDto dto) =>
        {
            var roomId = Guid.NewGuid();
            var roomCreatedAt = DateTimeOffset.UtcNow;

            var insertRoomsStatement = new SimpleStatement(
                "INSERT INTO rooms (room_id, name, description, created_at) VALUES (?, ?, ?, ?);",
                roomId, dto.Name, dto.Description, roomCreatedAt
            );

            var insertRoomsByUserStatement = new SimpleStatement(
                "INSERT INTO rooms_by_user (user_id, room_id, room_description, room_name) VALUES (?, ?, ?, ?);",
                dto.UserId, roomId, dto.Description, dto.Name
            );

            var defaultRoles = new HashSet<string> { "dictator for life" };
            var insertUsersByRoomStatement = new SimpleStatement(
                """
                INSERT INTO users_by_room (room_id, user_id, joined_at, roles, user_display_name)
                VALUES (?, ?, ?, ?, ?);
                """,
                roomId, dto.UserId, roomCreatedAt, defaultRoles, dto.UserDisplayName
            );

            var batch = new BatchStatement()
                .Add(insertRoomsStatement)
                .Add(insertRoomsByUserStatement)
                .Add(insertUsersByRoomStatement);

            await session.ExecuteAsync(batch);

            var roomResponse = new
            {
                room_id = roomId,
                name = dto.Name,
                description = dto.Description
            };
            return Results.Created($"/rooms/{roomId}", roomResponse);
        });

        app.MapGet("/{roomId:guid}/users", async
            (Guid roomId, DateTimeOffset? seenJoinedAt, int? pageSize) =>
        {
            pageSize = pageSize is > 0 ? pageSize.Value : 5;
            var statement =
                "SELECT user_id, user_display_name, joined_at, roles FROM users_by_room WHERE room_id = ?";
            if (seenJoinedAt.HasValue) statement += " AND joined_at > ?";

            statement += " LIMIT ?;";

            var selectUsersByRoomStatement = seenJoinedAt.HasValue
                ? new SimpleStatement(statement, roomId, seenJoinedAt, pageSize)
                : new SimpleStatement(statement, roomId, pageSize);

            var usersByRoom = await session.ExecuteAsync(selectUsersByRoomStatement);

            var users = usersByRoom.Select(row => new
            {
                UserId = row.GetValue<Guid>("user_id"),
                DisplayName = row.GetValue<string>("user_display_name"),
                JoinedAt = row.GetValue<DateTimeOffset>("joined_at"),
                Roles = row.GetValue<HashSet<string>>("roles")
            });

            return Results.Ok(users);
        });

        app.MapGet("/{roomId:guid}", async (Guid roomId) =>
        {
            var statement = new SimpleStatement(
                "SELECT room_id, name, description FROM rooms WHERE room_id = ?;",
                roomId
            );

            var row = await session.ExecuteAsync(statement);
            var room = row.FirstOrDefault();
            if (room == null) return Results.NotFound();

            return Results.Ok(new
            {
                RoomId = room.GetValue<Guid>("room_id"),
                Name = room.GetValue<string>("name"),
                Description = room.GetValue<string>("description")
            });
        });

        // NOTE: redis?
        app.MapGet("/{roomId:guid}/users/count", async (Guid roomId) =>
        {
            var statement = new SimpleStatement(
                "SELECT COUNT(*) FROM users_by_room WHERE room_id = ?;",
                roomId
            );

            var row = await session.ExecuteAsync(statement);
            var countValue = row.First().GetValue<long>("count");

            return Results.Ok(new
            {
                Count = countValue
            });
        });

        app.MapPost("/{roomId:guid}/users", async (Guid roomId, RoomJoinRequestDto dto) =>
        {
            var joinedAt = DateTimeOffset.UtcNow;
            var insertUsersByRoomStatement = new SimpleStatement(
                """
                INSERT INTO users_by_room (room_id, user_id, joined_at, roles, user_display_name)
                VALUES (?, ?, ?, ?, ?);
                """,
                roomId, dto.UserId, joinedAt, dto.Roles, dto.UserDisplayName
            );

            var insertRoomsByUserStatement = new SimpleStatement(
                "INSERT INTO rooms_by_user (user_id, room_id, room_description, room_name) VALUES (?, ?, ?, ?);",
                dto.UserId, roomId, dto.RoomDescription, dto.RoomName
            );

            var batch = new BatchStatement()
                .Add(insertUsersByRoomStatement)
                .Add(insertRoomsByUserStatement);

            await session.ExecuteAsync(batch);

            return Results.Created($"/rooms/{roomId}/users/{dto.UserId}", new
            {
                dto.UserId,
                joinedAt,
                dto.Roles,
                dto.UserDisplayName
            });
        });

        return app;
    }
}
