using Microsoft.Extensions.Configuration;

namespace TodoApi.AuthBootstrap;

public sealed class AuthBootstrapOptions
{
    public string TenantId { get; private init; } = string.Empty;

    public string ClientId { get; private init; } = string.Empty;

    public string[] Scopes { get; private init; } =
    [
        "https://graph.microsoft.com/Tasks.ReadWrite",
        "offline_access"
    ];

    public int DeviceCodeTimeoutSeconds { get; private init; } = 900;

    public static AuthBootstrapOptions FromConfiguration(IConfiguration configuration)
    {
        var graphSection = configuration.GetSection("TodoApi:Graph");
        var scopesValue = graphSection["Scopes"];
        var scopes = string.IsNullOrWhiteSpace(scopesValue)
            ? ["https://graph.microsoft.com/Tasks.ReadWrite", "offline_access"]
            : scopesValue
                .Split([' ', ',', ';', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

        var timeoutValue = graphSection["DeviceCodeTimeoutSeconds"];
        var timeoutSeconds = int.TryParse(timeoutValue, out var parsedTimeout) ? parsedTimeout : 900;

        return new AuthBootstrapOptions
        {
            TenantId = graphSection["TenantId"] ?? string.Empty,
            ClientId = graphSection["ClientId"] ?? string.Empty,
            Scopes = scopes,
            DeviceCodeTimeoutSeconds = timeoutSeconds
        };
    }

    public IReadOnlyList<string> Validate()
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(TenantId))
        {
            failures.Add("TodoApi:Graph:TenantId is required.");
        }

        if (string.IsNullOrWhiteSpace(ClientId))
        {
            failures.Add("TodoApi:Graph:ClientId is required.");
        }

        if (!Scopes.Contains("https://graph.microsoft.com/Tasks.ReadWrite", StringComparer.OrdinalIgnoreCase))
        {
            failures.Add("TodoApi:Graph:Scopes must include https://graph.microsoft.com/Tasks.ReadWrite.");
        }

        if (!Scopes.Contains("offline_access", StringComparer.OrdinalIgnoreCase))
        {
            failures.Add("TodoApi:Graph:Scopes must include offline_access.");
        }

        if (DeviceCodeTimeoutSeconds is < 60 or > 1800)
        {
            failures.Add("TodoApi:Graph:DeviceCodeTimeoutSeconds must be between 60 and 1800 seconds.");
        }

        return failures;
    }
}