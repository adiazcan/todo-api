using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using TodoApi.Functions.Options;

namespace TodoApi.Functions.Auth;

public sealed class MsalGraphAccessTokenProvider : IGraphAccessTokenProvider
{
    private readonly ILogger<MsalGraphAccessTokenProvider> _logger;
    private readonly GraphOptions _options;

    public MsalGraphAccessTokenProvider(IOptions<GraphOptions> options, ILogger<MsalGraphAccessTokenProvider> logger)
    {
        _logger = logger;
        _options = options.Value;
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        var app = ConfidentialClientApplicationBuilder
            .Create(_options.ClientId)
            .WithAuthority(AzureCloudInstance.AzurePublic, _options.TenantId)
            .WithClientSecret(_options.ClientSecret)
            .Build();

        _logger.LogDebug("Acquiring Microsoft Graph delegated access token from configured refresh token.");

        var result = await ((IByRefreshToken)app)
            .AcquireTokenByRefreshToken(_options.Scopes, _options.RefreshToken)
            .ExecuteAsync(cancellationToken)
            .ConfigureAwait(false);

        return result.AccessToken;
    }
}