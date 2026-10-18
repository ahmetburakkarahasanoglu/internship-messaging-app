using ChatApi.Dtos;

namespace ChatApi.Services;

public interface IMessageService
{
    Task<IReadOnlyList<MessageResponse>> GetForConversationAsync(int conversationId, int? after, CancellationToken cancellationToken);
    Task<MessageResponse> SendAsync(int conversationId, SendMessageRequest request, CancellationToken cancellationToken);
}
