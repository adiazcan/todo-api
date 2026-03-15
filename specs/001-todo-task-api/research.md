# Research: Create Microsoft To Do Task API

## Hosting Model

- Decision: Use Azure Functions isolated worker with .NET 10 and C#.
- Rationale: Current Azure Functions guidance recommends isolated worker for modern .NET versions, standard dependency injection, and better process control. It is also the supported path for non-LTS and newer .NET versions.
- Alternatives considered: In-process Azure Functions was rejected because support ends in 2026 and it is not the recommended path for new .NET function apps.

## Linux Hosting Plan

- Decision: Target Linux Flex Consumption or Premium for deployed environments.
- Rationale: Microsoft documents that .NET 10 apps cannot run on Linux in the classic Consumption plan. Flex Consumption preserves the serverless model while remaining compatible with Linux deployment.
- Alternatives considered: Classic Linux Consumption was rejected because it does not support .NET 10. Dedicated App Service was rejected as unnecessary for the current scope unless operational requirements later exceed Flex or Premium needs.

## Graph Integration Library

- Decision: Use the Microsoft Graph .NET SDK v5 to list task lists and create the task in Microsoft To Do.
- Rationale: The user explicitly requested the Graph SDK, and Microsoft guidance recommends using service SDKs directly in Azure Functions for runtime-determined operations because SDKs provide better coverage, error handling, and debugging than imperative bindings or handwritten HTTP calls.
- Alternatives considered: Raw HTTP calls to Microsoft Graph were rejected because they duplicate serialization, auth, and error-mapping concerns already handled by the SDK.

## Microsoft To Do Permission Model

- Decision: Use delegated `Tasks.ReadWrite` permission for the one configured Microsoft account.
- Rationale: Microsoft Graph documents that creating a Microsoft To Do task is supported with delegated `Tasks.ReadWrite` permission and does not support application permissions for this specific operation. That makes an app-only daemon approach invalid for task creation.
- Alternatives considered: Application permissions were rejected because the create-task API does not support them. Resource owner password credentials were rejected because they are not recommended and create unnecessary security and compatibility risk.

## Delegated Token Strategy

- Decision: Use an Entra app registration plus delegated user consent with `offline_access`, persist the serialized MSAL user token cache securely, and acquire access tokens at runtime via MSAL silent token acquisition before constructing the Graph client.
- Rationale: Microsoft identity documentation states that `offline_access` is required for long-lived delegated renewal capability and that MSAL manages refresh tokens inside its user token cache rather than exposing them directly. Persisting the serialized MSAL cache matches the bootstrap utility flow and still allows silent renewal without per-request user sign-in.
- Alternatives considered: On-behalf-of flow was rejected because the API caller is not expected to bring the configured account's user token. Direct raw refresh-token handling was rejected as the primary design because MSAL.NET does not expose refresh tokens for general copy-paste usage.

## Siri Shortcut Caller Authentication

- Decision: Authenticate Siri Shortcut calls to the Azure Function by using a function key or equivalent API key carried in the request, preferably in the `x-functions-key` header.
- Rationale: Apple Shortcuts can invoke HTTP endpoints with the Get Contents of URL action and set request details, while Azure Functions natively supports lightweight key-based protection for HTTP triggers through either the `code` query parameter or `x-functions-key` header. This keeps Siri invocation simple and avoids an interactive sign-in flow at shortcut runtime.
- Alternatives considered: Microsoft Entra sign-in directly from the shortcut was rejected because it adds browser-based OAuth complexity and would create a poor Siri experience. Anonymous endpoints were rejected because they remove caller authentication entirely.

## One-Time Graph Consent Bootstrap

- Decision: Perform Microsoft account authentication once through a local bootstrap console utility, using MSAL and a device-code delegated consent flow for `Tasks.ReadWrite` and `offline_access`, then store the resulting serialized MSAL user token cache securely for backend use.
- Rationale: This keeps setup simple for a personal Siri-driven workflow and avoids building or hosting a custom setup web page. The operator still completes Microsoft sign-in once, but only during setup. After that, the backend can continue operating silently by reusing the stored MSAL cache.
- Alternatives considered: A dedicated setup web page was rejected because the chosen direction is to avoid a hosted auth UI. Reauthenticating on every Siri call was rejected because it would make the shortcut impractical.

## Bootstrap Utility UX

- Decision: Use a console-based setup utility as the explicit operator workflow for first-time Microsoft consent.
- Rationale: A console utility is the smallest maintainable artifact that can guide the operator through first-run consent, persist the resulting serialized MSAL user token cache, and then exit. It satisfies the one-time setup requirement without introducing another deployed surface.
- Alternatives considered: Manual token copy/paste processes were rejected because they are brittle and easy to misconfigure.

## Runtime Token Renewal

- Decision: Use MSAL in the backend to silently renew Graph access tokens from the stored serialized user token cache before each Graph call.
- Rationale: Microsoft recommends MSAL for production token handling, and MSAL manages token caching and refresh behavior so the API can continue acting on behalf of the configured user without interactive sign-in.
- Alternatives considered: Manually coding raw token refresh requests was rejected because it adds avoidable protocol complexity and error-handling risk. A refresh-token-only configuration remains available only as a migration fallback.

## Unsuitable Auth Patterns For This Scenario

- Decision: Do not use on-behalf-of flow or direct user sign-in from the Siri Shortcut for the v1 design.
- Rationale: OBO requires the caller to already present a user access token to the API, which a Siri Shortcut is not expected to manage. Direct interactive OAuth from the shortcut would add repeated browser hops and make voice capture unreliable.
- Alternatives considered: None retained for v1 because both patterns conflict with the low-friction Siri requirement.

## List Selection Strategy

- Decision: Require the caller to provide the target Microsoft To Do `listId` as part of the request payload.
- Rationale: Making `listId` an explicit parameter gives the caller deterministic control over the destination list, removes default-list discovery from the hot path, and avoids relying on `wellknownListName` resolution for every request.
- Alternatives considered: Resolving `defaultList` dynamically was rejected because the user requested that the list ID be passed as a parameter instead of being inferred.

## API Surface

- Decision: Expose one HTTP POST endpoint at `/api/tasks` that accepts `text` and `listId` and returns a stable success/error envelope.
- Rationale: The feature scope remains narrow while allowing the caller to choose the destination list explicitly through a single required parameter.
- Alternatives considered: A broader REST surface with reminders, due dates, notes, or attachments was rejected because the specification explicitly keeps those out of scope.

## Validation and Error Mapping

- Decision: Trim leading and trailing whitespace from task text, reject missing or blank `text` and `listId` at the API boundary, and map Graph list-validation or task-creation failures to explicit dependency errors without reporting success unless Graph confirms task creation.
- Rationale: This directly satisfies the validation, deterministic outcome, and no-false-success requirements while minimizing hidden behavior.
- Alternatives considered: Passing Graph validation errors straight through without normalization was rejected because callers need a consistent envelope for automation.

## Test Strategy

- Decision: Implement three test layers: unit tests for validation and service logic, integration tests for the HTTP-triggered function, and contract tests for the published OpenAPI contract and Graph boundary behavior.
- Rationale: This satisfies the constitution and matches the feature's primary failure surfaces: request validation, endpoint behavior, and Microsoft Graph interaction.
- Alternatives considered: Integration-only coverage was rejected because it would make Graph-specific edge cases slower and harder to isolate; unit-only coverage was rejected because it would miss endpoint wiring and envelope guarantees.
