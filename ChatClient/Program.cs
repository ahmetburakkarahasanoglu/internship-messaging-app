using ChatClient.Api;
using ChatClient.Configuration;
using ChatClient.Ui;

if (!ClientOptions.TryParse(args, out var options, out var error))
{
    Console.Error.WriteLine(error);
    Console.Error.WriteLine("Kullanım: ChatClient [--api-url URL] [--poll-seconds 1-60]");
    return;
}

using var httpClient = new HttpClient
{
    BaseAddress = options.ApiBaseAddress,
    Timeout = TimeSpan.FromSeconds(10)
};

var api = new ChatApiClient(httpClient);
var application = new ChatApplication(api, options.PollingSeconds);
await application.RunAsync();
