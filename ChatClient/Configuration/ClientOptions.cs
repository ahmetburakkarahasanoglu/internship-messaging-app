using System.Diagnostics.CodeAnalysis;

namespace ChatClient.Configuration;

public sealed record ClientOptions(Uri ApiBaseAddress, int PollingSeconds)
{
    public static bool TryParse(string[] args, [NotNullWhen(true)] out ClientOptions? options, out string error)
    {
        var apiUrl = (string?)null;
        var pollingSeconds = 2;

        for (var index = 0; index < args.Length; index++)
        {
            var argument = args[index];
            var separatorIndex = argument.IndexOf('=');
            var option = separatorIndex >= 0 ? argument[..separatorIndex] : argument;
            var value = separatorIndex >= 0 ? argument[(separatorIndex + 1)..] : null;

            if (option is "--api-url" or "--poll-seconds")
            {
                if (value is null)
                {
                    if (index + 1 >= args.Length || args[index + 1].StartsWith("--", StringComparison.Ordinal))
                    {
                        options = null;
                        error = $"{option} seçeneği için bir değer belirtin.";
                        return false;
                    }

                    value = args[++index];
                }

                if (string.IsNullOrWhiteSpace(value))
                {
                    options = null;
                    error = $"{option} seçeneği için boş olmayan bir değer belirtin.";
                    return false;
                }

                if (option == "--api-url")
                {
                    if (apiUrl is not null)
                    {
                        options = null;
                        error = "--api-url seçeneği birden fazla kez kullanılamaz.";
                        return false;
                    }

                    apiUrl = value;
                    continue;
                }

                if (!int.TryParse(value, out pollingSeconds) || pollingSeconds is < 1 or > 60)
                {
                    options = null;
                    error = "--poll-seconds değeri 1 ile 60 arasında bir tam sayı olmalıdır.";
                    return false;
                }

                continue;
            }

            if (argument.StartsWith("--", StringComparison.Ordinal))
            {
                options = null;
                error = $"Bilinmeyen seçenek: {argument}";
                return false;
            }

            if (apiUrl is not null)
            {
                options = null;
                error = "API adresi yalnızca bir kez belirtilebilir.";
                return false;
            }

            apiUrl = argument;
        }

        apiUrl ??= "http://localhost:5000";
        if (!Uri.TryCreate(apiUrl, UriKind.Absolute, out var baseAddress) ||
            (baseAddress.Scheme != Uri.UriSchemeHttp && baseAddress.Scheme != Uri.UriSchemeHttps))
        {
            options = null;
            error = "API adresi geçersiz. Örnek: http://localhost:5000";
            return false;
        }

        if (!baseAddress.AbsoluteUri.EndsWith("/", StringComparison.Ordinal))
            baseAddress = new Uri(baseAddress.AbsoluteUri + "/");

        options = new ClientOptions(baseAddress, pollingSeconds);
        error = string.Empty;
        return true;
    }
}
