using System.ComponentModel.DataAnnotations;

namespace CassandraChat.Users;

public class User
{
    [Required] public Guid UserId { get; set; }
    public required string Email { get; set; }
    public required string Username { get; set; }
    [Required] public DateTimeOffset CreatedAt { get; set; }
}
