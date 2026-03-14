using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;
using TodoApi.AuthBootstrap;

var configuration = new ConfigurationBuilder()
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
	.WithDefaultRedirectUri()
	.Build();

using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(options.DeviceCodeTimeoutSeconds));

Console.WriteLine("Starting Microsoft Graph delegated consent bootstrap.");
Console.WriteLine($"Scopes: {string.Join(' ', options.Scopes)}");

var result = await publicClientApplication
	.AcquireTokenWithDeviceCode(options.Scopes, deviceCodeResult =>
	{
		Console.WriteLine(deviceCodeResult.Message);
		return Task.CompletedTask;
	})
	.ExecuteAsync(timeoutCts.Token);

Console.WriteLine($"Authenticated account: {result.Account.Username}");
Console.WriteLine($"Access token expires at: {result.ExpiresOn:O}");
Console.WriteLine("Interactive consent completed. Persisting refresh-token material is left for the next implementation slice.");

return 0;