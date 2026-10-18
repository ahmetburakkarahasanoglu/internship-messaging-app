namespace ChatApi.Entities;

public sealed class Message
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public required string Sender { get; set; }
    public required string Content { get; set; }
    public DateTime SentAt { get; set; }
    public Conversation Conversation { get; set; } = null!;
}
