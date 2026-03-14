# todo-api

A Microsoft To Do task-creation API for Siri integration.

## Planned Structure

The repository is organized around one Azure Functions isolated worker app, one bootstrap console utility, and three test projects:

```text
src/TodoApi.Functions/
tools/TodoApi.AuthBootstrap/
tests/TodoApi.UnitTests/
tests/TodoApi.IntegrationTests/
tests/TodoApi.ContractTests/
```

## Local Configuration

Copy `src/TodoApi.Functions/local.settings.json.example` to `src/TodoApi.Functions/local.settings.json` for local development and provide values for the Microsoft Graph delegated account configuration.

Keep real secrets out of source control. Use local-only settings during development and secret-backed application settings or Key Vault references in deployed environments.

## Initial App Settings

The function app expects these settings during local development:

```json
{
	"IsEncrypted": false,
	"Values": {
		"AzureWebJobsStorage": "UseDevelopmentStorage=true",
		"FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
		"TodoApi__Graph__TenantId": "<tenant-id>",
		"TodoApi__Graph__ClientId": "<client-id>",
		"TodoApi__Graph__ClientSecret": "<client-secret>",
		"TodoApi__Graph__RefreshToken": "<refresh-token>",
		"TodoApi__Graph__Scopes": "https://graph.microsoft.com/Tasks.ReadWrite offline_access"
	}
}
```

## Test Projects

The repository uses separate unit, integration, and contract test projects so request validation, function behavior, and published contract coverage can evolve independently.
