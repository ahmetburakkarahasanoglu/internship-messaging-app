namespace ChatApi.Entities;

public sealed class Conversation
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<Message> Messages { get; set; } = [];
}
