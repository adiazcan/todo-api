# todo-api

A Microsoft To Do task-creation API for Siri integration. The app exposes one `POST /api/tasks` Azure Functions endpoint, validates a strict v1 payload, and creates exactly one Microsoft To Do task in the caller-selected list for one preconfigured Microsoft account.

## Repository Layout

```text
src/TodoApi.Functions/
tools/TodoApi.AuthBootstrap/
tests/TodoApi.UnitTests/
tests/TodoApi.IntegrationTests/
tests/TodoApi.ContractTests/
specs/001-todo-task-api/
```

## Local Setup

1. Register a Microsoft Entra app with delegated Microsoft Graph permission `Tasks.ReadWrite` and request `offline_access` during consent.
2. Run the bootstrap utility once for the Microsoft account that should own created tasks:

```bash
dotnet run --project tools/TodoApi.AuthBootstrap
```

The bootstrap utility uses device-code sign-in, confirms delegated consent for the configured account, and prints the serialized MSAL user token cache that the function app should store securely. See `tools/TodoApi.AuthBootstrap/README.md` for configuration and installation guidance.

3. Copy `src/TodoApi.Functions/local.settings.json.example` to `src/TodoApi.Functions/local.settings.json` and install local values for the Graph configuration.
4. Start Azurite if you are using local storage emulation.
5. Start the function host from the repository root or the functions project directory.

```bash
func start --csharp
```

6. Invoke the endpoint with the local function key.

```bash
curl -X POST http://localhost:7071/api/tasks \
	-H "Content-Type: application/json" \
	-H "x-functions-key: <local-function-key>" \
	-d '{"listId":"AQMkAGI2...","text":"Buy milk"}'
```

Expected success result: HTTP `201 Created` with `success`, `taskId`, `title`, `listId`, `listName`, and optional `createdAtUtc` fields.

## Configuration

The function app expects these settings during local development or deployment:

```json
{
	"IsEncrypted": false,
	"Values": {
		"AzureWebJobsStorage": "UseDevelopmentStorage=true",
		"AzureWebJobsSecretStorageType": "files",
		"FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
		"TodoApi__Graph__TenantId": "<tenant-id>",
		"TodoApi__Graph__ClientId": "<client-id>",
		"TodoApi__Graph__UserTokenCache": "<base64-msal-user-token-cache>",
		"TodoApi__Graph__AccountUsername": "<configured-account-username>",
		"TodoApi__Graph__Scopes": "https://graph.microsoft.com/Tasks.ReadWrite offline_access",
		"TodoApi__Graph__BaseUrl": "https://graph.microsoft.com/v1.0",
		"TodoApi__Graph__RequestTimeoutSeconds": "3",
		"TodoApi__Graph__MaxRetryAttempts": "2"
	}
}
```

Keep secrets out of source control. For deployed environments, prefer Azure Key Vault references or equivalent secret-backed application settings.

Legacy migration path: the function app still accepts `TodoApi__Graph__RefreshToken` plus `TodoApi__Graph__ClientSecret`, but the bootstrap utility now emits `TodoApi__Graph__UserTokenCache` and `TodoApi__Graph__AccountUsername` instead.

## Deployment Notes

- Deploy the .NET 10 isolated worker app to Azure Functions on Linux Flex Consumption or Premium. Classic Linux Consumption is out of scope because it does not support this runtime target.
- Keep the endpoint protected with a function key at minimum, and prefer `x-functions-key` for callers such as Siri Shortcuts.
- Preserve the 3-second Graph timeout and bounded retry defaults unless you have measured evidence that a different budget is required.
- Enforce HTTPS-only traffic and install Graph secrets through app settings or Key Vault references, not plaintext files.

## GitHub Actions Deployment

The repository includes a deployment workflow at `.github/workflows/deploy-functions.yml` that builds the solution, runs the test suite, publishes `src/TodoApi.Functions`, and deploys the published output to Azure Functions.

The workflow uses OpenID Connect with `azure/login@v2`, which is the recommended authentication model for Azure Functions deployments. Before the workflow can deploy, configure Azure and GitHub with these values:

1. Create a user-assigned managed identity in Azure.
2. Assign the `Website Contributor` role to that identity on the target Function App.
3. Add a federated credential on the managed identity for this GitHub repository and the branch that should deploy, typically `main`.
4. Add these repository variables in GitHub Actions:
	- `AZURE_CLIENT_ID`
	- `AZURE_TENANT_ID`
	- `AZURE_SUBSCRIPTION_ID`
	- `AZURE_FUNCTIONAPP_NAME`

After that setup, deployments run automatically on pushes to `main` that affect the function app or the workflow, and can also be started manually with `workflow_dispatch`.

## Validation Status

The repository uses separate unit, integration, and contract suites so validation, endpoint behavior, Graph boundary translation, and payload compatibility stay isolated.

Run the full validation set with:

```bash
dotnet test TodoApi.sln
```

Phase 6 coverage adds these checks:

- Contract tests verify the published OpenAPI surface, the Graph request and response translation boundary, and the Siri Shortcuts-compatible request payload shape.
- Integration tests verify success and failure behavior, single-create guarantees, and a local 100-request burst completing inside the 5-second target with a mocked task service.
- Quickstart validation expects blank or malformed payloads to return `400`, missing authorization to return `401`, non-transient upstream failures to return `502`, and transient token or Graph failures to return `503`.

## First-Time Integrator Flow

1. Bootstrap delegated consent for the target account.
2. Install the serialized MSAL user token cache and account username in local settings for development or app settings for Azure.
3. Start the function host and invoke `POST /api/tasks` with `x-functions-key`.
4. Confirm the returned `listId` matches the requested list and only one task is created per successful request.
