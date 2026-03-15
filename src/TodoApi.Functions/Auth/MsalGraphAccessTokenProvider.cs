using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using TodoApi.Functions.Options;

namespace TodoApi.Functions.Auth;

public sealed class MsalGraphAccessTokenProvider : IGraphAccessTokenProvider
{
    private readonly ILogger<MsalGraphAccessTokenProvider> _logger;
    private readonly GraphOptions _options;
    private readonly IClientApplicationBase _clientApplication;

    public MsalGraphAccessTokenProvider(IOptions<GraphOptions> options, ILogger<MsalGraphAccessTokenProvider> logger)
    {
        _logger = logger;
        _options = options.Value;
        _clientApplication = CreateClientApplication(_options);
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(_options.UserTokenCache))
        {
            _logger.LogDebug("Acquiring Microsoft Graph delegated access token from the serialized MSAL user token cache.");

            var account = await ResolveAccountAsync(cancellationToken).ConfigureAwait(false);
            var result = await _clientApplication
                .AcquireTokenSilent(_options.Scopes, account)
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);

            return result.AccessToken;
        }

        _logger.LogDebug("Acquiring Microsoft Graph delegated access token from configured refresh token fallback.");

        var refreshResult = await ((IByRefreshToken)_clientApplication)
            .AcquireTokenByRefreshToken(_options.Scopes, _options.RefreshToken)
            .ExecuteAsync(cancellationToken)
            .ConfigureAwait(false);

        return refreshResult.AccessToken;
    }

    private async Task<IAccount> ResolveAccountAsync(CancellationToken cancellationToken)
    {
        var accounts = (await _clientApplication.GetAccountsAsync().ConfigureAwait(false)).ToArray();
        if (accounts.Length == 0)
        {
            throw new MsalUiRequiredException("no_cached_account", "No cached Microsoft Graph account is available. Run the bootstrap utility again.");
        }

        if (!string.IsNullOrWhiteSpace(_options.AccountUsername))
        {
            var matchingAccount = accounts.FirstOrDefault(account =>
                string.Equals(account.Username, _options.AccountUsername, StringComparison.OrdinalIgnoreCase));

            if (matchingAccount is null)
            {
                throw new MsalUiRequiredException(
                    "configured_account_not_found",
                    $"The configured account '{_options.AccountUsername}' was not found in the serialized MSAL token cache.");
            }

            return matchingAccount;
        }

        if (accounts.Length > 1)
        {
            throw new MsalUiRequiredException(
                "multiple_cached_accounts",
                "Multiple Microsoft Graph accounts exist in the serialized MSAL token cache. Configure TodoApi:Graph:AccountUsername.");
        }

        return accounts[0];
    }

    private static IClientApplicationBase CreateClientApplication(GraphOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.UserTokenCache))
        {
            var app = PublicClientApplicationBuilder
                .Create(options.ClientId)
                .WithAuthority(AzureCloudInstance.AzurePublic, options.TenantId)
                .Build();

            new SerializedTokenCache(app.UserTokenCache, options.UserTokenCache);
            return app;
        }

        return ConfidentialClientApplicationBuilder
            .Create(options.ClientId)
            .WithAuthority(AzureCloudInstance.AzurePublic, options.TenantId)
            .WithClientSecret(options.ClientSecret)
            .Build();
    }

    private sealed class SerializedTokenCache
    {
        private readonly object _syncRoot = new();
        private byte[] _serializedCacheBytes;

        public SerializedTokenCache(ITokenCache tokenCache, string serializedTokenCache)
        {
            _serializedCacheBytes = Convert.FromBase64String(serializedTokenCache);
            tokenCache.SetBeforeAccess(DeserializeCache);
            tokenCache.SetAfterAccess(PersistLatestCache);
        }

        private void DeserializeCache(TokenCacheNotificationArgs args)
        {
            lock (_syncRoot)
            {
                args.TokenCache.DeserializeMsalV3(_serializedCacheBytes, shouldClearExistingCache: true);
            }
        }

        private void PersistLatestCache(TokenCacheNotificationArgs args)
        {
            if (!args.HasStateChanged)
            {
                return;
            }

            lock (_syncRoot)
            {
                _serializedCacheBytes = args.TokenCache.SerializeMsalV3();
            }
        }
    }
}