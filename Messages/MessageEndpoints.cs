using Cassandra;
using Isession = Cassandra.ISession;

namespace CassandraChat.Messages;

public static class MessageEndpoints
{
    public static RouteGroupBuilder MapMessagesApi(this RouteGroupBuilder app, Isession session)
    {
        app.MapPost("/{roomId:guid}/messages", async
            (Guid roomId, MessageCreateRequestDto dto) =>
        {
            var messageId = Guid.NewGuid();
            var createdAt = DateTimeOffset.UtcNow;

            var insertMessageByRoomStatement = new SimpleStatement(
                """
                INSERT INTO messages_by_room (room_id, created_at, message_id, sender_id, content, is_pinned, sender_name)
                VALUES (?, ?, ?, ?, ?, ?, ?);
                """,
                roomId, createdAt, messageId, dto.SenderId, dto.Content, false, dto.SenderName
            );

            var insertMessageByRoomAndUserStatement = new SimpleStatement(
                """
                INSERT INTO messages_by_room_and_sender (room_id, sender_id, created_at, message_id, content, is_pinned, sender_name)
                VALUES (?, ?, ?, ?, ?, ?, ?);
                """,
                roomId, dto.SenderId, createdAt, messageId, dto.Content, false, dto.SenderName
            );

            var batch = new BatchStatement()
                .Add(insertMessageByRoomStatement)
                .Add(insertMessageByRoomAndUserStatement);

            await session.ExecuteAsync(batch);

            return Results.Created($"/{roomId}/messages/{messageId}", new
            {
                messageId,
                dto.SenderId,
                dto.SenderName,
                dto.Content,
                createdAt
            });
        });

        app.MapGet("/{roomId:guid}/messages", async
            (Guid roomId, DateTimeOffset? seenCreatedAt, int? pageSize) =>
        {
            pageSize = pageSize is > 0 ? pageSize.Value : 100;
            var statement =
                "SELECT message_id, created_at, content, sender_id, sender_name FROM messages_by_room WHERE room_id = ?";
            if (seenCreatedAt.HasValue)
            {
                statement += " AND created_at < ?";
            }

            statement += " LIMIT ?";

            var query = seenCreatedAt.HasValue
                ? new SimpleStatement(statement, roomId, seenCreatedAt, pageSize)
                : new SimpleStatement(statement, roomId, pageSize);

            var result = await session.ExecuteAsync(query);

            var messages = result.Select(row => new
            {
                messageId = row.GetValue<Guid>("message_id"),
                createAt = row.GetValue<DateTimeOffset>("created_at"),
                content = row.GetValue<string>("content"),
                senderId = row.GetValue<Guid>("sender_id"),
                senderName = row.GetValue<string>("sender_name"),
            });

            return Results.Ok(messages);
        });

        app.MapGet("/{roomId:guid}/messages/{senderId:guid}", async
            (Guid roomId, Guid senderId, DateTimeOffset? seenCreatedAt, int? pageSize) =>
        {
            pageSize = pageSize is > 0 ? pageSize.Value : 100;
            var statement = """
                            SELECT message_id, created_at, sender_name, content FROM messages_by_room_and_sender
                            WHERE room_id = ? AND sender_id = ?
                            """;
            if (seenCreatedAt.HasValue)
            {
                statement += " AND created_at < ?";
            }

            statement += " LIMIT ?";

            var query = seenCreatedAt.HasValue
                ? new SimpleStatement(statement, roomId, senderId, seenCreatedAt, pageSize)
                : new SimpleStatement(statement, roomId, senderId, pageSize);

            var result = await session.ExecuteAsync(query);

            var messages = result.Select(row => new
            {
                messageId = row.GetValue<Guid>("message_id"),
                createAt = row.GetValue<DateTimeOffset>("created_at"),
                content = row.GetValue<string>("content"),
                senderName = row.GetValue<string>("sender_name"),
            });

            return Results.Ok(messages);
        });

        app.MapPut("/{roomId:guid}/messages/{messageId:guid}", async
            (Guid roomId, Guid messageId, MessageUpdateRequestDto dto) =>
        {
            var updatesMessageByRoom = new SimpleStatement(
                "UPDATE messages_by_room SET content = ?, sender_name = ?" +
                " WHERE room_id = ? AND created_at = ? AND message_id = ?",
                dto.Content, dto.SenderName, roomId, dto.CreatedAt, messageId
            );

            var updateMessageByRoomAndSender = new SimpleStatement(
                "UPDATE messages_by_room_and_sender SET content = ?, sender_name = ?" +
                " WHERE room_id = ? AND sender_id = ? AND message_id = ? AND created_at = ?",
                dto.Content, dto.SenderName, roomId, dto.SenderId, messageId, dto.CreatedAt
            );

            var batch = new BatchStatement()
                .Add(updatesMessageByRoom)
                .Add(updateMessageByRoomAndSender);

            await session.ExecuteAsync(batch);

            return Results.Ok(new
            {
                dto.SenderId,
                dto.SenderName,
                dto.Content,
                dto.CreatedAt
            });
        });

        return app;
    }
}
