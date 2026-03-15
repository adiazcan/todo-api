# TodoApi.AuthBootstrap

`TodoApi.AuthBootstrap` is the operator-facing console utility for the one-time delegated Microsoft Graph consent flow used by the API. It runs a device-code sign-in for the Microsoft account that should own created Microsoft To Do tasks.

## What It Does

- Validates the required Graph bootstrap configuration.
- Starts a device-code sign-in flow for the configured Microsoft account.
- Requests delegated `Tasks.ReadWrite` and `offline_access` scopes.
- Confirms the authenticated account and token expiry details after consent succeeds.
- Emits a Base64-encoded MSAL user token cache payload and account username for function app configuration.

The bootstrap utility does not expose a raw refresh token. Instead, it emits the serialized MSAL user token cache that the function app can later reuse with `AcquireTokenSilent`.

## Required Configuration

Provide these values through environment variables or `appsettings.json` for the console app:

```json
{
  "TodoApi": {
    "Graph": {
      "TenantId": "<tenant-id>",
      "ClientId": "<client-id>",
      "Scopes": "https://graph.microsoft.com/Tasks.ReadWrite offline_access",
      "DeviceCodeTimeoutSeconds": 900
    }
  }
}
```

## Run The Utility

From the repository root:

```bash
dotnet run --project tools/TodoApi.AuthBootstrap
```

Expected flow:

1. The utility prints the Graph scopes it will request.
2. Microsoft identity returns a device code and verification URL.
3. You complete sign-in and consent in the browser for the exact account that should own created tasks.
4. The utility prints the authenticated account, token expiry timestamp, and the configuration keys to install in the function app.

## Secret Installation Guidance

After consent succeeds, install the delegated Graph settings for the function app in one place only:

- Local development: `src/TodoApi.Functions/local.settings.json`
- Deployed environments: Azure Functions application settings or Azure Key Vault references

The function app expects these keys:

- `TodoApi__Graph__TenantId`
- `TodoApi__Graph__ClientId`
- `TodoApi__Graph__UserTokenCache`
- `TodoApi__Graph__AccountUsername`
- `TodoApi__Graph__Scopes`
- `TodoApi__Graph__BaseUrl`
- `TodoApi__Graph__RequestTimeoutSeconds`
- `TodoApi__Graph__MaxRetryAttempts`

Legacy fallback:

- `TodoApi__Graph__RefreshToken`
- `TodoApi__Graph__ClientSecret`

Recommended operator practice:

1. Keep the serialized user token cache in a secret store and do not commit it to source control.
2. Re-run the bootstrap flow if the configured Microsoft account is forced to re-consent.
3. Replace the stored `TodoApi__Graph__UserTokenCache` immediately after any re-bootstrap event.

## Troubleshooting

- Missing `TenantId` or `ClientId`: the utility exits with validation errors before starting the device-code flow.
- Missing `offline_access` or `Tasks.ReadWrite`: update the configured scopes and retry.
- Device-code timeout: increase `DeviceCodeTimeoutSeconds` up to the supported validation limit if your operator workflow needs more time.
- Device-code flow fails with `invalid_client`: enable public client flows on the Entra application registration, then rerun the utility.
- Auth succeeds but the API later returns `401`: re-bootstrap the account and replace the stored `TodoApi__Graph__UserTokenCache` value.