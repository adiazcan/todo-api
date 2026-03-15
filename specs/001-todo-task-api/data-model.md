# Data Model: Create Microsoft To Do Task API

## TaskCreateRequest

- Purpose: Transport model for the single public API operation.
- Fields:
  - `text` (`string`, required): Raw task text supplied by the caller.
  - `listId` (`string`, required): Microsoft To Do list identifier that should receive the created task.
- Validation rules:
  - Required.
  - After trimming leading and trailing whitespace, value must not be empty.
  - `listId` must be present and non-blank after trimming.
  - No additional request fields are accepted in v1.
- State transitions:
  - `received` -> `normalized` when whitespace is trimmed.
  - `normalized` -> `rejected` when resulting text is empty.
  - `normalized` -> `accepted` when text remains non-empty.

## NormalizedTaskCommand

- Purpose: Internal command passed from the HTTP layer to the task creation service.
- Fields:
  - `title` (`string`, required): Trimmed task title to persist in Microsoft To Do.
  - `listId` (`string`, required): Target list identifier supplied by the caller.
  - `requestedAtUtc` (`datetime`, required): UTC timestamp captured when request handling begins.
- Validation rules:
  - `title` must equal the caller input after trimming only; no other mutation is allowed.
  - `listId` must equal the caller input after trimming only.

## ConfiguredAccountContext

- Purpose: Runtime configuration representing the single Microsoft account this API acts for.
- Fields:
  - `tenantId` (`string`, required)
  - `clientId` (`string`, required)
  - `clientSecret` (`secret reference`, optional for legacy refresh-token fallback)
  - `userIdOrUpn` (`string`, optional): Stored only if needed for diagnostics or future `/users/{id}` Graph paths.
  - `userTokenCache` (`secret`, required): Serialized MSAL user token cache for the configured account.
  - `scopes` (`string[]`, required): Must include `Tasks.ReadWrite` and `offline_access` during consent/bootstrap.
- Validation rules:
  - All secrets must be loaded from secret storage, not source-controlled files.
  - Configuration must identify exactly one account for the feature.

## TargetTodoList

- Purpose: Microsoft To Do destination list referenced by the caller and validated against Graph.
- Fields:
  - `id` (`string`, required)
  - `displayName` (`string`, required)
  - `isOwner` (`boolean`, optional)
  - `isShared` (`boolean`, optional)
- Validation rules:
  - The selected list must exist and be accessible by the configured account.
- Relationships:
  - One `ConfiguredAccountContext` validates one caller-supplied target list per execution.

## CreatedTodoTask

- Purpose: Canonical representation of the task returned by Microsoft Graph after successful creation.
- Fields:
  - `id` (`string`, required)
  - `title` (`string`, required)
  - `createdDateTime` (`datetime`, optional if returned by Graph)
  - `webUrl` (`string`, optional if returned by Graph)
  - `listId` (`string`, required)
  - `listName` (`string`, required)
- Validation rules:
  - `title` must match the normalized request title.
  - `id` must be present before the API can emit success.
- Relationships:
  - Produced from one `NormalizedTaskCommand` and one `TargetTodoList`.

## TaskCreateResponse

- Purpose: Success envelope returned to the caller.
- Fields:
  - `success` (`boolean`, required): Always `true` in this model.
  - `data.taskId` (`string`, required)
  - `data.title` (`string`, required)
  - `data.listId` (`string`, required)
  - `data.listName` (`string`, required)
  - `data.createdAtUtc` (`datetime`, optional)
- Validation rules:
  - Only emitted when one Graph create call completes successfully.

## ErrorResponse

- Purpose: Stable failure envelope for validation, auth, and upstream dependency errors.
- Fields:
  - `success` (`boolean`, required): Always `false` in this model.
  - `error.code` (`string`, required)
  - `error.message` (`string`, required)
  - `error.retryable` (`boolean`, required)
  - `error.details` (`object`, optional): Non-secret diagnostics safe for callers.
- Validation rules:
  - Must never contain secrets, tokens, or raw upstream headers.
  - Must distinguish caller-correctable validation failures from dependency failures.

## Entity Relationships Summary

- One `TaskCreateRequest` becomes one `NormalizedTaskCommand` after trim validation.
- One `ConfiguredAccountContext` validates one `TargetTodoList` identified by the submitted `listId`.
- One accepted command can produce exactly one `CreatedTodoTask`.
- The API emits either one `TaskCreateResponse` or one `ErrorResponse` for each request.