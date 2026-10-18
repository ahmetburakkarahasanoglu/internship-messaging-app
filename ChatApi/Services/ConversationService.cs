using ChatApi.Data;
using ChatApi.Dtos;
using ChatApi.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChatApi.Services;

public sealed class ConversationService(ChatDbContext dbContext) : IConversationService
{
    public async Task<ConversationResponse> CreateAsync(
        CreateConversationRequest request,
        CancellationToken cancellationToken)
    {
        var title = request.Title?.Trim();
        if (string.IsNullOrWhiteSpace(title))
            throw new ChatValidationException("Sohbet başlığı boş olamaz.");
        if (title.Length > 100)
            throw new ChatValidationException("Sohbet başlığı en fazla 100 karakter olabilir.");

        var conversation = new Conversation
        {
            Title = title,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Conversations.Add(conversation);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Map(conversation);
    }

    public async Task<IReadOnlyList<ConversationResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Conversations
            .AsNoTracking()
            .OrderByDescending(conversation => conversation.CreatedAt)
            .Select(conversation => new ConversationResponse(
                conversation.Id,
                conversation.Title,
                conversation.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<ConversationDetailResponse> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var conversation = await dbContext.Conversations
            .AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new
            {
                item.Id,
                item.Title,
                item.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (conversation is null)
            throw new ResourceNotFoundException($"{id} numaralı sohbet bulunamadı.");

        var messages = dbContext.Messages.AsNoTracking().Where(message => message.ConversationId == id);
        var messageCount = await messages.CountAsync(cancellationToken);
        var participants = await messages
            .Select(message => message.Sender)
            .Distinct()
            .OrderBy(sender => sender)
            .ToListAsync(cancellationToken);

        return new ConversationDetailResponse(
            conversation.Id,
            conversation.Title,
            conversation.CreatedAt,
            messageCount,
            participants);
    }

    private static ConversationResponse Map(Conversation conversation) =>
        new(conversation.Id, conversation.Title, conversation.CreatedAt);
}
