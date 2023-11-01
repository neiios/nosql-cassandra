using System.ComponentModel.DataAnnotations;

namespace CassandraChat.Rooms;

public class RoomJoinRequestDto
{
    [Required] public Guid UserId { get; set; }
    public required string RoomDescription { get; set; }
    public required string RoomName { get; set; }
    public required string UserDisplayName { get; set; }
    public required HashSet<string> Roles { get; set; }
}
