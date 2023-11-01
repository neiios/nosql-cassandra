using System.ComponentModel.DataAnnotations;

namespace CassandraChat.Messages;

public class MessageDeleteRequestDto
{
    [Required] public DateTimeOffset CreatedAt { get; set; }
    [Required] public Guid SenderId { get; set; }
}
