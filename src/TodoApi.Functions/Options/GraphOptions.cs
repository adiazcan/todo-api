namespace TodoApi.Functions.Options;

public sealed class GraphOptions
{
    public const string SectionName = "TodoApi:Graph";
    public const string DefaultTasksReadWriteScope = "https://graph.microsoft.com/Tasks.ReadWrite";
    public const string OfflineAccessScope = "offline_access";

    public string TenantId { get; set; } = string.Empty;

    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    public string UserTokenCache { get; set; } = string.Empty;

    public string AccountUsername { get; set; } = string.Empty;

    public string RefreshToken { get; set; } = string.Empty;

    public string[] Scopes { get; set; } = [DefaultTasksReadWriteScope, OfflineAccessScope];

    public string BaseUrl { get; set; } = "https://graph.microsoft.com/v1.0";

    public int RequestTimeoutSeconds { get; set; } = 3;

    public int MaxRetryAttempts { get; set; } = 2;
}