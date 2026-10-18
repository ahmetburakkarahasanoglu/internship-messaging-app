namespace ChatApi.Dtos;

public sealed record SendMessageRequest(string? Sender, string? Content);
