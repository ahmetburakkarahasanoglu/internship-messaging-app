using ChatApi.Dtos;

namespace ChatApi.Services;

public interface IConversationService
{
    Task<ConversationResponse> CreateAsync(CreateConversationRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<ConversationResponse>> GetAllAsync(CancellationToken cancellationToken);
    Task<ConversationDetailResponse> GetByIdAsync(int id, CancellationToken cancellationToken);
}
