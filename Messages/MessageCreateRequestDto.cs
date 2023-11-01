using System.ComponentModel.DataAnnotations;

namespace CassandraChat.Messages;

public class MessageCreateRequestDto
{
    [Required] public Guid SenderId { get; set; }
    public required string SenderName { get; set; }
    public required string Content { get; set; }
}
