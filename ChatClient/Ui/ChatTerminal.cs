using System.Text;
using ChatClient.Dtos;

namespace ChatClient.Ui;

public sealed class ChatTerminal
{
    private readonly object _consoleLock = new();
    private readonly string _prompt;
    private readonly StringBuilder _input = new();

    public ChatTerminal(string sender) => _prompt = $"{sender} > ";

    public void PrintMessage(MessageResponse message)
    {
        lock (_consoleLock)
        {
            ClearInputRow();
            Console.WriteLine($"[{message.SentAt.ToLocalTime():HH:mm:ss}] {message.Sender}: {message.Content}");
            RenderInputRow();
        }
    }

    public void PrintStatus(string text)
    {
        lock (_consoleLock)
        {
            ClearInputRow();
            Console.WriteLine(text);
            RenderInputRow();
        }
    }

    public async Task<string?> ReadMessageAsync(CancellationToken cancellationToken)
    {
        if (Console.IsInputRedirected)
        {
            lock (_consoleLock)
                Console.Write(_prompt);
            return await Console.In.ReadLineAsync(cancellationToken);
        }

        lock (_consoleLock)
            RenderInputRow();

        while (!cancellationToken.IsCancellationRequested)
        {
            var key = Console.ReadKey(intercept: true);
            lock (_consoleLock)
            {
                if (key.Key == ConsoleKey.Enter)
                {
                    var result = _input.ToString();
                    _input.Clear();
                    Console.WriteLine();
                    RenderInputRow();
                    return result;
                }

                if (key.Key == ConsoleKey.Backspace)
                {
                    if (_input.Length > 0)
                        _input.Length--;
                    RenderInputRow();
                    continue;
                }

                if (!char.IsControl(key.KeyChar) && _input.Length < 2000)
                {
                    _input.Append(key.KeyChar);
                    RenderInputRow();
                }
            }
        }

        return null;
    }

    private void ClearInputRow()
    {
        if (Console.IsOutputRedirected)
            return;
        Console.Write('\r');
        Console.Write(new string(' ', Math.Max(1, Console.WindowWidth - 1)));
        Console.Write('\r');
    }

    private void RenderInputRow()
    {
        Console.Write('\r');
        if (!Console.IsOutputRedirected)
            Console.Write(new string(' ', Math.Max(1, Console.WindowWidth - 1)));
        Console.Write('\r');

        var available = Console.IsOutputRedirected ? 120 : Math.Max(10, Console.WindowWidth - 1);
        var value = _prompt + _input;
        Console.Write(value.Length <= available ? value : value[^available..]);
    }
}
