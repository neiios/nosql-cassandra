using System.ComponentModel.DataAnnotations;

namespace CassandraChat.Users;

public class UserCreateRequestDto
{
    [EmailAddress] public required string Email { get; set; }
    public required string Password { get; set; }
    public required string Username { get; set; }
}