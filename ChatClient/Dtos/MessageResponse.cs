namespace ChatClient.Dtos;

public sealed record MessageResponse(int Id, int ConversationId, string Sender, string Content, DateTime SentAt);
