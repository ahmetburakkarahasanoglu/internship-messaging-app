namespace ChatApi.Dtos;

public sealed record ConversationDetailResponse(
    int Id,
    string Title,
    DateTime CreatedAt,
    int MessageCount,
    IReadOnlyList<string> Participants);
