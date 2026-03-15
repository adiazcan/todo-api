using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;
using TodoApi.AuthBootstrap;

var configuration = new ConfigurationBuilder()
	.SetBasePath(AppContext.BaseDirectory)
	.AddJsonFile("appsettings.json", optional: true)
	.AddEnvironmentVariables()
	.Build();

var options = AuthBootstrapOptions.FromConfiguration(configuration);
var validationErrors = options.Validate();
if (validationErrors.Count > 0)
{
	Console.Error.WriteLine("TodoApi.AuthBootstrap configuration is invalid:");
	foreach (var validationError in validationErrors)
	{
		Console.Error.WriteLine($"- {validationError}");
	}

	return 1;
}

var publicClientApplication = PublicClientApplicationBuilder
	.Create(options.ClientId)
	.WithAuthority(AzureCloudInstance.AzurePublic, options.TenantId)
	.Build();

var tokenCacheStore = new SerializedTokenCacheStore(publicClientApplication.UserTokenCache);

using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(options.DeviceCodeTimeoutSeconds));

Console.WriteLine("Starting Microsoft Graph delegated consent bootstrap.");
Console.WriteLine($"Scopes: {string.Join(' ', options.Scopes)}");

AuthenticationResult result;
try
{
	result = await publicClientApplication
		.AcquireTokenWithDeviceCode(options.Scopes, deviceCodeResult =>
		{
			Console.WriteLine(deviceCodeResult.Message);
			return Task.CompletedTask;
		})
		.ExecuteAsync(timeoutCts.Token);
}
catch (MsalServiceException exception) when (string.Equals(exception.ErrorCode, "invalid_client", StringComparison.OrdinalIgnoreCase))
{
	Console.Error.WriteLine("Device-code bootstrap failed because the Microsoft Entra application is not enabled for public client flows.");
	Console.Error.WriteLine("Enable mobile and desktop or public client flows on the app registration, then rerun the bootstrap utility.");
	return 1;
}

Console.WriteLine($"Authenticated account: {result.Account.Username}");
Console.WriteLine($"Access token expires at: {result.ExpiresOn:O}");

var serializedUserTokenCache = tokenCacheStore.ExportBase64();
Console.WriteLine("Interactive consent completed. Install the following settings in the function app configuration:");
Console.WriteLine($"TodoApi__Graph__UserTokenCache={serializedUserTokenCache}");
Console.WriteLine($"TodoApi__Graph__AccountUsername={result.Account.Username}");

return 0;

file sealed class SerializedTokenCacheStore
{
	private readonly object _syncRoot = new();
	private byte[] _cacheBytes = [];

	public SerializedTokenCacheStore(ITokenCache tokenCache)
	{
		tokenCache.SetBeforeAccess(DeserializeCache);
		tokenCache.SetAfterAccess(SerializeCache);
	}

	public string ExportBase64()
	{
		lock (_syncRoot)
		{
			return Convert.ToBase64String(_cacheBytes);
		}
	}

	private void DeserializeCache(TokenCacheNotificationArgs args)
	{
		lock (_syncRoot)
		{
			if (_cacheBytes.Length > 0)
			{
				args.TokenCache.DeserializeMsalV3(_cacheBytes, shouldClearExistingCache: true);
			}
		}
	}

	private void SerializeCache(TokenCacheNotificationArgs args)
	{
		if (!args.HasStateChanged)
		{
			return;
		}

		lock (_syncRoot)
		{
			_cacheBytes = args.TokenCache.SerializeMsalV3();
		}
	}
}