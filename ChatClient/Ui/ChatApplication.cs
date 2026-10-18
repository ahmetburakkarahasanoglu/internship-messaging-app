using System.Net;
using ChatClient.Api;
using ChatClient.Dtos;

namespace ChatClient.Ui;

public sealed class ChatApplication(ChatApiClient api, int pollingSeconds)
{
    public async Task RunAsync()
    {
        var username = await ReadRequiredLineAsync("Kullanıcı adınız: ", 50);
        if (username is null)
            return;
        username = username.Trim();

        Console.WriteLine($"API: {api.BaseAddress}");
        while (true)
        {
            Console.WriteLine("\n1) Sohbetleri listele\n2) Yeni sohbet oluştur\n3) Sohbete gir\n0) Çıkış");
            Console.Write("Seçiminiz: ");
            var choice = Console.ReadLine()?.Trim();

            try
            {
                switch (choice)
                {
                    case "1":
                        await ListConversationsAsync();
                        break;
                    case "2":
                        await CreateConversationAsync();
                        break;
                    case "3":
                        await EnterConversationAsync(username);
                        break;
                    case "0":
                        return;
                    default:
                        Console.WriteLine("Menüden 0, 1, 2 veya 3 seçin.");
                        break;
                }
            }
            catch (ApiException exception)
            {
                WriteApiError(exception);
            }
            catch (HttpRequestException)
            {
                Console.WriteLine("API'ye ulaşılamadı. Sunucunun çalıştığını ve adresi kontrol edin.");
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("API isteği zaman aşımına uğradı.");
            }
        }
    }

    private async Task ListConversationsAsync()
    {
        var conversations = await api.GetConversationsAsync();
        if (conversations.Count == 0)
        {
            Console.WriteLine("Henüz sohbet yok.");
            return;
        }

        foreach (var conversation in conversations)
            Console.WriteLine($"{conversation.Id}) {conversation.Title} ({conversation.CreatedAt.ToLocalTime():g})");
    }

    private async Task CreateConversationAsync()
    {
        var title = await ReadRequiredLineAsync("Sohbet başlığı: ", 100);
        if (title is null)
            return;

        var conversation = await api.CreateConversationAsync(title.Trim());
        Console.WriteLine($"Sohbet oluşturuldu: {conversation.Id}) {conversation.Title}");
    }

    private async Task EnterConversationAsync(string username)
    {
        var conversations = await api.GetConversationsAsync();
        if (conversations.Count == 0)
        {
            Console.WriteLine("Henüz sohbet yok. Önce menüden yeni bir sohbet oluşturun.");
            return;
        }

        Console.WriteLine("Mevcut sohbetler:");
        foreach (var conversation in conversations)
            Console.WriteLine($"{conversation.Id}) {conversation.Title}");

        Console.Write("Sohbet numarası: ");
        if (!int.TryParse(Console.ReadLine(), out var conversationId) || conversationId <= 0)
        {
            Console.WriteLine("Geçerli ve pozitif bir sohbet numarası girin.");
            return;
        }

        var detail = await api.GetConversationAsync(conversationId);
        var history = await api.GetMessagesAsync(conversationId);
        Console.WriteLine($"\n#{detail.Id} {detail.Title} — {detail.MessageCount} mesaj, katılımcılar: " +
                          (detail.Participants.Count == 0 ? "henüz yok" : string.Join(", ", detail.Participants)));
        foreach (var message in history)
            PrintMessage(message);

        Console.WriteLine("Mesaj yazıp Enter'a basın. Sohbetten çıkmak için /back yazın.");
        var tracker = new MessageTracker();
        tracker.Seed(history);
        var terminal = new ChatTerminal(username);
        using var pollingCancellation = new CancellationTokenSource();
        var pollTask = PollMessagesAsync(conversationId, tracker, terminal, pollingCancellation.Token);

        try
        {
            while (true)
            {
                var content = await terminal.ReadMessageAsync(pollingCancellation.Token);
                if (content is null || content.Trim().Equals("/back", StringComparison.OrdinalIgnoreCase))
                    break;
                if (string.IsNullOrWhiteSpace(content))
                {
                    terminal.PrintStatus("Boş mesaj gönderilemez.");
                    continue;
                }
                if (content.Length > 2000)
                {
                    terminal.PrintStatus("Mesaj en fazla 2000 karakter olabilir.");
                    continue;
                }

                try
                {
                    var sentMessage = await api.SendMessageAsync(
                        conversationId,
                        username,
                        content,
                        pollingCancellation.Token);
                    if (tracker.Accept(sentMessage))
                        terminal.PrintMessage(sentMessage);
                }
                catch (ApiException exception)
                {
                    terminal.PrintStatus($"Gönderilemedi: {exception.Message}");
                }
                catch (HttpRequestException)
                {
                    terminal.PrintStatus("API'ye ulaşılamadı; mesaj gönderilemedi.");
                }
                catch (TaskCanceledException) when (!pollingCancellation.IsCancellationRequested)
                {
                    terminal.PrintStatus("İstek zaman aşımına uğradı; mesaj gönderilemedi.");
                }
            }
        }
        finally
        {
            pollingCancellation.Cancel();
            try
            {
                await pollTask;
            }
            catch (OperationCanceledException)
            {
            }
        }
    }

    private async Task PollMessagesAsync(
        int conversationId,
        MessageTracker tracker,
        ChatTerminal terminal,
        CancellationToken cancellationToken)
    {
        var reportedFailure = false;
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var messages = await api.GetMessagesAsync(conversationId, tracker.LastId, cancellationToken);
                foreach (var message in messages)
                {
                    if (tracker.Accept(message))
                        terminal.PrintMessage(message);
                }
                reportedFailure = false;
            }
            catch (HttpRequestException) when (!cancellationToken.IsCancellationRequested)
            {
                if (!reportedFailure)
                    terminal.PrintStatus("Sunucuya bağlanılamadı; yeniden denenecek.");
                reportedFailure = true;
            }
            catch (ApiException exception) when (!cancellationToken.IsCancellationRequested)
            {
                if (!reportedFailure)
                    terminal.PrintStatus($"Yeni mesajlar alınamadı: {exception.Message}");
                reportedFailure = true;
            }

            await Task.Delay(TimeSpan.FromSeconds(pollingSeconds), cancellationToken);
        }
    }

    private static void PrintMessage(MessageResponse message) =>
        Console.WriteLine($"[{message.SentAt.ToLocalTime():HH:mm:ss}] {message.Sender}: {message.Content}");

    private static async Task<string?> ReadRequiredLineAsync(string prompt, int maxLength)
    {
        while (true)
        {
            Console.Write(prompt);
            var value = await Console.In.ReadLineAsync();
            if (value is null)
                return null;
            if (string.IsNullOrWhiteSpace(value))
            {
                Console.WriteLine("Bu alan boş bırakılamaz.");
                continue;
            }
            if (value.Trim().Length > maxLength)
            {
                Console.WriteLine($"En fazla {maxLength} karakter girin.");
                continue;
            }
            return value;
        }
    }

    private static void WriteApiError(ApiException exception)
    {
        var reason = exception.StatusCode switch
        {
            HttpStatusCode.NotFound => "İstenen sohbet bulunamadı.",
            HttpStatusCode.BadRequest => "İstek geçersiz.",
            _ => $"API hatası ({(int)exception.StatusCode})."
        };
        Console.WriteLine($"{reason} {exception.Message}");
    }
}
