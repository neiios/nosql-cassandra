namespace CassandraChat.Users;

public class UserResponseDto
{
    public Guid UserId { get; set; }
    public string? Email { get; set; }
    public string? Username { get; set; }
}
