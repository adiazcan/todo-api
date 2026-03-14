using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace TodoApi.Functions.Options;

public sealed class GraphOptionsSetup : IConfigureNamedOptions<GraphOptions>, IValidateOptions<GraphOptions>
{
    private static readonly string[] RequiredScopes =
    [
        GraphOptions.DefaultTasksReadWriteScope,
        GraphOptions.OfflineAccessScope
    ];

    private readonly IConfiguration _configuration;

    public GraphOptionsSetup(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void Configure(GraphOptions options)
    {
        Configure(Microsoft.Extensions.Options.Options.DefaultName, options);
    }

    public void Configure(string? name, GraphOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var section = _configuration.GetSection(GraphOptions.SectionName);
        section.Bind(options);

        var configuredScopes = ReadScopes(section);
        if (configuredScopes.Length > 0)
        {
            options.Scopes = configuredScopes;
        }
    }

    public ValidateOptionsResult Validate(string? name, GraphOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.TenantId))
        {
            failures.Add("TodoApi:Graph:TenantId is required.");
        }

        if (string.IsNullOrWhiteSpace(options.ClientId))
        {
            failures.Add("TodoApi:Graph:ClientId is required.");
        }

        if (string.IsNullOrWhiteSpace(options.ClientSecret))
        {
            failures.Add("TodoApi:Graph:ClientSecret is required.");
        }

        if (string.IsNullOrWhiteSpace(options.RefreshToken))
        {
            failures.Add("TodoApi:Graph:RefreshToken is required.");
        }

        if (options.Scopes.Length == 0)
        {
            failures.Add("TodoApi:Graph:Scopes must include delegated Microsoft Graph scopes.");
        }
        else
        {
            foreach (var requiredScope in RequiredScopes)
            {
                if (!options.Scopes.Contains(requiredScope, StringComparer.OrdinalIgnoreCase))
                {
                    failures.Add($"TodoApi:Graph:Scopes must include '{requiredScope}'.");
                }
            }
        }

        if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _))
        {
            failures.Add("TodoApi:Graph:BaseUrl must be an absolute URI.");
        }

        if (options.RequestTimeoutSeconds is < 1 or > 30)
        {
            failures.Add("TodoApi:Graph:RequestTimeoutSeconds must be between 1 and 30 seconds.");
        }

        if (options.MaxRetryAttempts is < 0 or > 5)
        {
            failures.Add("TodoApi:Graph:MaxRetryAttempts must be between 0 and 5.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static string[] ReadScopes(IConfigurationSection section)
    {
        var rawScopeValue = section["Scopes"];
        if (!string.IsNullOrWhiteSpace(rawScopeValue))
        {
            return ParseScopes(rawScopeValue);
        }

        var scopeChildren = section.GetSection("Scopes").GetChildren().Select(child => child.Value).OfType<string>();
        return scopeChildren.Where(scope => !string.IsNullOrWhiteSpace(scope)).Select(scope => scope.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static string[] ParseScopes(string rawScopeValue)
    {
        return rawScopeValue
            .Split([' ', ',', ';', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}