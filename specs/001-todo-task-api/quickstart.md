# Quickstart: Create Microsoft To Do Task API

## Prerequisites

- .NET 10 SDK installed locally.
- Azure Functions Core Tools v4 installed.
- An Azure subscription for deployment.
- A Microsoft Entra app registration allowed to request delegated Microsoft Graph permissions.
- A Microsoft account or work/school account that will be the single configured Microsoft To Do destination account.

## 1. Register the Microsoft identity application

1. Create an app registration in Microsoft Entra ID.
2. Add Microsoft Graph delegated permission `Tasks.ReadWrite`.
3. Ensure the authorization flow also requests `offline_access` so the bootstrap flow returns a refresh token.
4. Create a client secret for the app if the runtime will use a confidential client.
5. Record the tenant ID, client ID, and client secret secret reference.

## 2. Bootstrap delegated access for the configured account

1. Run the one-time console utility at `tools/TodoApi.AuthBootstrap` for the exact Microsoft account this API should write tasks to.
2. Let the utility start the Microsoft sign-in flow and request delegated `Tasks.ReadWrite` plus `offline_access`.
3. Complete consent once when prompted.
4. Let the utility persist the resulting token material to secure storage or export it for secure installation in your backend secrets.

Expected result: the function app can refresh Graph access tokens for the configured account without prompting on each request.

Recommended implementation note: this bootstrap is done by a local console utility, not by the Siri Shortcut and not by a permanent web page.

## 3. Prepare local configuration

Create `/home/adiazcan/github/todo-api/src/TodoApi.Functions/local.settings.json` from an example file during implementation with values similar to the following:

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
    "TodoApi__Graph__Scopes": "https://graph.microsoft.com/Tasks.ReadWrite"
  }
}
```

For deployed environments, place secrets in Azure Key Vault or equivalent secret-backed app settings instead of plaintext files.

## 4. Implement the function app structure

Create the planned project layout:

```text
/home/adiazcan/github/todo-api/src/TodoApi.Functions/
├── Program.cs
├── host.json
├── local.settings.json.example
├── Functions/
├── Contracts/
├── Auth/
├── Options/
└── Services/

/home/adiazcan/github/todo-api/tools/TodoApi.AuthBootstrap/
└── Program.cs
```

Implementation expectations:

- `Functions/`: HTTP-triggered endpoint at `POST /api/tasks` with function-level auth.
- `Contracts/`: Request and response DTOs matching `contracts/openapi.yaml`.
- `Auth/`: MSAL-backed token acquisition and Graph client factory.
- `Services/`: Target-list validation and task creation orchestration.
- `Options/`: Strongly typed configuration binding for Graph and runtime settings.
- `tools/TodoApi.AuthBootstrap`: One-time operator utility that acquires delegated consent and writes token material for the backend.

## 5. Run locally

1. Start Azurite if using local storage emulation.
2. Start the function host from `/home/adiazcan/github/todo-api/src/TodoApi.Functions`.
3. Send a test request using the function key as the caller credential:

```bash
curl -X POST http://localhost:7071/api/tasks \
  -H "Content-Type: application/json" \
  -H "x-functions-key: <local-function-key>" \
  -d '{"listId":"AQMkAGI2...","text":"Buy milk"}'
```

Expected result: HTTP `201` with a success payload containing the created task ID and the selected list metadata.

## 5A. Configure the Siri Shortcut

1. Use Shortcuts' Get Contents of URL action to call the deployed API endpoint.
2. Set method to `POST`.
3. Set `Content-Type: application/json`.
4. Include the Azure Functions key in the `x-functions-key` header.
5. Send a JSON body with `listId` and `text`.

Expected result: Siri can invoke the API without performing an OAuth sign-in flow each time.

## 6. Test before implementation completion

Write tests first to satisfy the constitution:

1. Unit tests for trimming, empty-text rejection, and Graph error mapping.
2. Integration tests for `POST /api/tasks` success and representative failure responses.
3. Contract tests that validate request and response payloads against `contracts/openapi.yaml`.

## 7. Deploy

1. Provision an Azure Functions app on Linux Flex Consumption or Premium.
2. Configure app settings or Key Vault references for Graph and token secrets.
3. Enforce HTTPS-only traffic.
4. Publish the .NET 10 isolated worker function app.
5. Verify with a live POST to `/api/tasks` using the function key.

## Operational Notes

- The Siri Shortcut should authenticate only to your API, not directly to Microsoft Graph.
- No custom setup web page is required for v1; Microsoft sign-in happens only inside the one-time bootstrap utility flow.
- The backend is responsible for refreshing Graph tokens silently by using the stored delegated refresh token.
- If the refresh token is revoked or expires without renewal, the API must fail clearly and require re-bootstrap of the configured account.
- If the submitted list ID is invalid or inaccessible for the configured account, treat the request as failed and do not create tasks in any fallback list.
- If future requirements add alternate lists or richer task fields, publish a versioned contract rather than changing this v1 request shape in place.