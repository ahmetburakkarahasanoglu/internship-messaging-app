namespace ChatClient.Dtos;

public sealed record CreateConversationRequest(string Title);
public sealed record SendMessageRequest(string Sender, string Content);
