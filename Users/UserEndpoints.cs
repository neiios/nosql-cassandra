using Cassandra;
using ISession = Cassandra.ISession;

namespace CassandraChat.Users;

public static class UserEndpoints
{
    public static RouteGroupBuilder MapUsersApi(this RouteGroupBuilder app, ISession session)
    {
        app.MapPost("/login", async (ILogger<Program> log, UserCreateRequestDto dto) =>
        {
            var statement = new SimpleStatement(
                "SELECT password_hash, user_id FROM users_by_email WHERE email = ?",
                dto.Email
            );
            var row = await session.ExecuteAsync(statement);
            var user = row.FirstOrDefault();
            if (user == null)
            {
                log.LogInformation("user {email} not found", dto.Email);
                return Results.NotFound();
            }

            var passwordHash = user.GetValue<string>("password_hash");
            if (!BCrypt.Net.BCrypt.Verify(dto.Password, passwordHash))
            {
                log.LogInformation("user {email} provided invalid password", dto.Email);
                return Results.Unauthorized();
            }

            var userId = user.GetValue<Guid>("user_id");
            statement = new SimpleStatement(
                "SELECT email, username FROM users WHERE user_id = ?",
                userId
            );
            row = await session.ExecuteAsync(statement);
            user = row.FirstOrDefault();
            if (user == null)
            {
                log.LogInformation("user id {email} not found", dto.Email);
                return Results.NotFound();
            }

            var userResponse = new
            {
                UserId = userId,
                Email = user.GetValue<string>("email"),
                Username = user.GetValue<string>("username")
            };
            return Results.Ok(userResponse);
        });

        app.MapPost("/register", async (ILogger<Program> log, UserCreateRequestDto dto) =>
        {
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            var userId = Guid.NewGuid();

            var statement = new SimpleStatement(
                "INSERT INTO users_by_email (email, password_hash, user_id) VALUES (?, ?, ?) IF NOT EXISTS;",
                dto.Email, passwordHash, userId
            );

            var result = await session.ExecuteAsync(statement);
            var row = result.FirstOrDefault();
            if (row != null && row.GetValue<bool>("[applied]") == false)
            {
                log.LogInformation("user email {email} already exists", dto.Email);
                return Results.Conflict(new { message = "user already exists" });
            }

            statement = new SimpleStatement(
                "INSERT INTO users (user_id, created_at, email, username)" +
                "VALUES (?, ?, ?, ?);",
                userId, DateTimeOffset.UtcNow, dto.Email, dto.Username
            );
            try
            {
                await session.ExecuteAsync(statement);
            }
            catch (Exception e)
            {
                log.LogCritical(e, "failed to create a user. email: {email}", dto.Email);
                return Results.Conflict(new { message = "failed to create user" });
            }

            return Results.Created($"/api/v1/users/{userId}", new
            {
                userId,
                dto.Email,
                dto.Username
            });
        });

        app.MapGet("/{userId:guid}", async (ILogger<Program> log, Guid userId) =>
        {
            var statement = new SimpleStatement(
                "SELECT user_id, email, username FROM users WHERE user_id = ?",
                userId
            );
            var row = await session.ExecuteAsync(statement);
            var user = row.FirstOrDefault();
            if (user == null)
            {
                log.LogInformation("user {userId} not found", userId);
                return Results.NotFound();
            }

            return Results.Ok(new
            {
                UserId = user.GetValue<Guid>("user_id"),
                Email = user.GetValue<string>("email"),
                Username = user.GetValue<string>("username")
            });
        });

        app.MapGet("/{userId:guid}/rooms", async (ILogger<Program> log, Guid userId) =>
        {
            var selectRoomsByUserStatement = new SimpleStatement(
                "SELECT room_id, room_name, room_description FROM rooms_by_user WHERE user_id = ?;",
                userId
            );

            var roomsByUser = await session.ExecuteAsync(selectRoomsByUserStatement);
            log.LogInformation("get rooms for user {userId}", userId);

            var rooms = roomsByUser.Select(row => new
            {
                RoomId = row.GetValue<Guid>("room_id"),
                Name = row.GetValue<string>("room_name"),
                Description = row.GetValue<string>("room_description")
            });

            return Results.Ok(rooms);
        });

        return app;
    }
}
