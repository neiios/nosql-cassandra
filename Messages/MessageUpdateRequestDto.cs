using System.ComponentModel.DataAnnotations;

namespace CassandraChat.Messages;

public class MessageUpdateRequestDto
{
    [Required] public DateTimeOffset CreatedAt { get; set; }
    [Required] public Guid SenderId { get; set; }
    public required string SenderName { get; set; }
    public required string Content { get; set; }
}
