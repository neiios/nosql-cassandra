namespace CassandraChat.Rooms;

public class RoomCreateRequestDto
{
    public required string Name { get; set; }
    public required string Description { get; set; }
    public Guid UserId { get; set; }
    public required string UserDisplayName { get; set; }
}
