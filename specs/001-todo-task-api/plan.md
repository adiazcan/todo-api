# Implementation Plan: Create Microsoft To Do Task API

**Branch**: `[001-todo-task-api]` | **Date**: 2026-03-14 | **Spec**: `/home/adiazcan/github/todo-api/specs/001-todo-task-api/spec.md`
**Input**: Feature specification from `/home/adiazcan/github/todo-api/specs/001-todo-task-api/spec.md`

## Summary

Build a single-endpoint serverless API with Azure Functions isolated worker on .NET 10 that accepts task text and a To Do list ID, validates and normalizes both inputs, and creates exactly one task in the specified Microsoft To Do list through the Microsoft Graph .NET SDK. Because Microsoft To Do task creation requires delegated `Tasks.ReadWrite` permission and does not support application permissions for this operation, the design uses a preconfigured delegated user session for one Microsoft account, with token refresh material provisioned once by a bootstrap console utility and retrieved by the function at runtime.

## Technical Context

**Language/Version**: C# on .NET 10 using the Azure Functions isolated worker model  
**Primary Dependencies**: Azure Functions Worker SDK, Azure Functions HTTP + ASP.NET Core integration packages, Microsoft.Graph v5 SDK, Microsoft.Identity.Client for delegated token acquisition and refresh, Azure.Identity/Azure Key Vault client for secret retrieval when deployed  
**Storage**: N/A for feature data; configuration and delegated token material stored as application settings locally and in Azure Key Vault or app settings in deployed environments  
**Testing**: xUnit with FluentAssertions for unit tests, ASP.NET Core/Azure Functions host-backed integration tests for HTTP behavior, contract tests against the OpenAPI document, a mocked Microsoft Graph boundary, and the Siri Shortcuts-compatible payload schema  
**Target Platform**: Azure Functions on Linux Flex Consumption or Premium; local development on Linux with Azure Functions Core Tools v4  
**Project Type**: Serverless HTTP API  
**Performance Goals**: Warm-path API p95 <= 500 ms and p99 <= 2000 ms for local validation/serialization overhead; end-to-end successful task creation visible in Microsoft To Do within 5 seconds; support 100 concurrent requests at feature scope  
**Constraints**: HTTP-triggered endpoint must require authentication via function key or stronger front-door auth; external Microsoft Graph calls capped at 3 second timeout with bounded retries; exactly one task per successful request; deployed Linux hosting must avoid classic Consumption because .NET 10 is unsupported there; only one preconfigured Microsoft account is in scope; the submitted list ID must be validated against that account before task creation  
**Scale/Scope**: One POST endpoint, one configured Microsoft To Do account, caller-supplied list IDs within that account, Siri-style low-volume capture traffic with occasional bursts up to 100 concurrent requests

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Initial Gate

- [x] **I. Code Quality** — Design splits concerns into HTTP function, request validation, Graph authentication provider, target-list resolver, and task creation service; dependency set is limited to Azure Functions, Graph SDK, identity, and test libraries required by the feature.
- [x] **II. Testing Standards** — Plan includes unit tests for validation and service logic, integration tests for success and representative Microsoft To Do failure paths, and contract tests for the HTTP surface, mocked Graph boundary, and Siri Shortcuts-compatible payload schema; implementation phase will require tests to be written first.
- [x] **III. UX Consistency** — Contract standardizes a success/error envelope, ISO 8601 UTC timestamps, and RFC 9110-compatible status codes for validation, auth, upstream dependency, and success outcomes.
- [x] **IV. Performance Requirements** — Design keeps the endpoint synchronous and bounded, sets a 3 second Graph timeout budget, validates the supplied list ID with bounded upstream calls, and avoids long-running function behavior.
- [x] **Security** — Endpoint requires a function key at minimum, secrets remain in environment-backed secret stores, request data is validated before any Graph call, and deployment guidance assumes HTTPS-only Azure Functions endpoints.

### Post-Design Re-check

- [x] **I. Code Quality** — Data model, contract, and quickstart preserve SRP boundaries and avoid speculative features outside task creation.
- [x] **II. Testing Standards** — Design artifacts identify the exact unit, integration, and contract suites required for the feature.
- [x] **III. UX Consistency** — Contract and quickstart use one stable route, one request shape, and one error envelope.
- [x] **IV. Performance Requirements** — Research-backed hosting and timeout decisions remain compatible with the target latency and concurrency constraints.
- [x] **Security** — The delegated-auth requirement is explicit, and token material handling is constrained to secret storage rather than source-controlled config.

## Project Structure

### Documentation (this feature)

```text
/home/adiazcan/github/todo-api/specs/001-todo-task-api/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── openapi.yaml
└── tasks.md
```

### Source Code (repository root)

```text
/home/adiazcan/github/todo-api/
├── src/
│   └── TodoApi.Functions/
│       ├── Program.cs
│       ├── host.json
│       ├── local.settings.json.example
│       ├── Functions/
│       ├── Contracts/
│       ├── Auth/
│       ├── Options/
│       └── Services/
├── tools/
│   └── TodoApi.AuthBootstrap/
│       └── Program.cs
└── tests/
    ├── TodoApi.UnitTests/
    ├── TodoApi.IntegrationTests/
    └── TodoApi.ContractTests/
```

**Structure Decision**: Use a single Azure Functions project under `src/TodoApi.Functions` with separate folders for transport contracts, delegated Graph auth, configuration options, and domain services. Add a small `tools/TodoApi.AuthBootstrap` console utility for the one-time delegated-consent setup so a permanent web page is not required. Keep test suites split by responsibility to satisfy the constitution's unit, integration, and contract testing requirements without introducing another hosted component.

## Complexity Tracking

No constitution violations are required for this design.
