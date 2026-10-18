using ChatApi.Data;
using ChatApi.Dtos;
using ChatApi.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChatApi.Services;

public sealed class MessageService(ChatDbContext dbContext) : IMessageService
{
    public async Task<IReadOnlyList<MessageResponse>> GetForConversationAsync(
        int conversationId,
        int? after,
        CancellationToken cancellationToken)
    {
        if (after < 0)
            throw new ChatValidationException("after değeri sıfır veya pozitif olmalıdır.");

        var conversationExists = await dbContext.Conversations
            .AnyAsync(conversation => conversation.Id == conversationId, cancellationToken);
        if (!conversationExists)
            throw new ResourceNotFoundException($"{conversationId} numaralı sohbet bulunamadı.");

        var query = dbContext.Messages
            .AsNoTracking()
            .Where(message => message.ConversationId == conversationId);
        if (after.HasValue)
            query = query.Where(message => message.Id > after.Value);

        return await query
            .OrderBy(message => message.Id)
            .Select(message => new MessageResponse(
                message.Id,
                message.ConversationId,
                message.Sender,
                message.Content,
                message.SentAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<MessageResponse> SendAsync(
        int conversationId,
        SendMessageRequest request,
        CancellationToken cancellationToken)
    {
        var sender = request.Sender?.Trim();
        if (string.IsNullOrWhiteSpace(sender))
            throw new ChatValidationException("Gönderen adı boş olamaz.");
        if (sender.Length > 50)
            throw new ChatValidationException("Gönderen adı en fazla 50 karakter olabilir.");
        if (string.IsNullOrWhiteSpace(request.Content))
            throw new ChatValidationException("Mesaj içeriği boş olamaz.");
        if (request.Content.Length > 2000)
            throw new ChatValidationException("Mesaj içeriği en fazla 2000 karakter olabilir.");

        var conversationExists = await dbContext.Conversations
            .AnyAsync(conversation => conversation.Id == conversationId, cancellationToken);
        if (!conversationExists)
            throw new ResourceNotFoundException($"{conversationId} numaralı sohbet bulunamadı.");

        var message = new Message
        {
            ConversationId = conversationId,
            Sender = sender,
            Content = request.Content,
            SentAt = DateTime.UtcNow
        };

        dbContext.Messages.Add(message);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new MessageResponse(message.Id, message.ConversationId, message.Sender, message.Content, message.SentAt);
    }
}
