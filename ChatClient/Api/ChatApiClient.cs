using System.Net.Http.Json;
using ChatClient.Dtos;

namespace ChatClient.Api;

public sealed class ChatApiClient(HttpClient httpClient)
{
    public Uri? BaseAddress => httpClient.BaseAddress;

    public async Task<IReadOnlyList<ConversationResponse>> GetConversationsAsync(CancellationToken cancellationToken = default) =>
        await SendAsync<IReadOnlyList<ConversationResponse>>(
            () => httpClient.GetAsync("api/conversations", cancellationToken), cancellationToken);

    public async Task<ConversationDetailResponse> GetConversationAsync(int id, CancellationToken cancellationToken = default) =>
        await SendAsync<ConversationDetailResponse>(
            () => httpClient.GetAsync($"api/conversations/{id}", cancellationToken), cancellationToken);

    public async Task<ConversationResponse> CreateConversationAsync(string title, CancellationToken cancellationToken = default) =>
        await SendAsync<ConversationResponse>(
            () => httpClient.PostAsJsonAsync("api/conversations", new CreateConversationRequest(title), cancellationToken),
            cancellationToken);

    public async Task<IReadOnlyList<MessageResponse>> GetMessagesAsync(
        int conversationId,
        int? after = null,
        CancellationToken cancellationToken = default)
    {
        var path = $"api/conversations/{conversationId}/messages";
        if (after.HasValue)
            path += $"?after={after.Value}";
        return await SendAsync<IReadOnlyList<MessageResponse>>(
            () => httpClient.GetAsync(path, cancellationToken), cancellationToken);
    }

    public async Task<MessageResponse> SendMessageAsync(
        int conversationId,
        string sender,
        string content,
        CancellationToken cancellationToken = default) =>
        await SendAsync<MessageResponse>(
            () => httpClient.PostAsJsonAsync(
                $"api/conversations/{conversationId}/messages",
                new SendMessageRequest(sender, content),
                cancellationToken),
            cancellationToken);

    private static async Task<T> SendAsync<T>(
        Func<Task<HttpResponseMessage>> send,
        CancellationToken cancellationToken)
    {
        using var response = await send();
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var detail = TryGetProblemDetail(body) ?? $"Sunucu {(int)response.StatusCode} ({response.ReasonPhrase}) yanıtı verdi.";
            throw new ApiException(response.StatusCode, detail);
        }

        var result = await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
        return result ?? throw new InvalidOperationException("Sunucudan boş yanıt alındı.");
    }

    private static string? TryGetProblemDetail(string body)
    {
        try
        {
            using var json = System.Text.Json.JsonDocument.Parse(body);
            var root = json.RootElement;
            if (root.TryGetProperty("detail", out var detail) && !string.IsNullOrWhiteSpace(detail.GetString()))
                return detail.GetString();
            if (root.TryGetProperty("title", out var title) && !string.IsNullOrWhiteSpace(title.GetString()))
                return title.GetString();
        }
        catch (System.Text.Json.JsonException)
        {
            // Non-ProblemDetails responses use the generic status message.
        }

        return null;
    }
}
