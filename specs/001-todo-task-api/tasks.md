# Tasks: Create Microsoft To Do Task API

**Input**: Design documents from `/specs/001-todo-task-api/`
**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/openapi.yaml`, `quickstart.md`

**Tests**: Unit, integration, and contract tests are mandatory for this feature. Write the tests for each user story first and confirm they fail before implementing the story.

**Organization**: Tasks are grouped by user story so each increment remains independently testable.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create the solution, projects, and baseline configuration required for implementation.

- [x] T001 Create the solution and project skeleton in `/home/adiazcan/github/todo-api/TodoApi.sln`, `/home/adiazcan/github/todo-api/src/TodoApi.Functions/TodoApi.Functions.csproj`, `/home/adiazcan/github/todo-api/tools/TodoApi.AuthBootstrap/TodoApi.AuthBootstrap.csproj`, `/home/adiazcan/github/todo-api/tests/TodoApi.UnitTests/TodoApi.UnitTests.csproj`, `/home/adiazcan/github/todo-api/tests/TodoApi.IntegrationTests/TodoApi.IntegrationTests.csproj`, and `/home/adiazcan/github/todo-api/tests/TodoApi.ContractTests/TodoApi.ContractTests.csproj`
- [x] T002 Configure the Azure Functions isolated worker host and package references in `/home/adiazcan/github/todo-api/src/TodoApi.Functions/Program.cs` and `/home/adiazcan/github/todo-api/src/TodoApi.Functions/host.json`
- [x] T003 [P] Add local configuration and secret-handling examples in `/home/adiazcan/github/todo-api/src/TodoApi.Functions/local.settings.json.example` and `/home/adiazcan/github/todo-api/README.md`
- [x] T004 [P] Add test SDK, FluentAssertions, and host-backed test dependencies in `/home/adiazcan/github/todo-api/tests/TodoApi.UnitTests/TodoApi.UnitTests.csproj`, `/home/adiazcan/github/todo-api/tests/TodoApi.IntegrationTests/TodoApi.IntegrationTests.csproj`, and `/home/adiazcan/github/todo-api/tests/TodoApi.ContractTests/TodoApi.ContractTests.csproj`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Build the shared configuration, auth, contract, and error-mapping infrastructure that all user stories depend on.

**⚠️ CRITICAL**: Complete this phase before starting user story work.

- [x] T005 Create Graph runtime options and configuration validation in `/home/adiazcan/github/todo-api/src/TodoApi.Functions/Options/GraphOptions.cs` and `/home/adiazcan/github/todo-api/src/TodoApi.Functions/Options/GraphOptionsSetup.cs`
- [x] T006 [P] Create shared request and response DTOs in `/home/adiazcan/github/todo-api/src/TodoApi.Functions/Contracts/CreateTaskRequest.cs`, `/home/adiazcan/github/todo-api/src/TodoApi.Functions/Contracts/CreateTaskSuccessResponse.cs`, and `/home/adiazcan/github/todo-api/src/TodoApi.Functions/Contracts/ErrorResponse.cs`
- [x] T007 [P] Implement delegated token acquisition abstractions in `/home/adiazcan/github/todo-api/src/TodoApi.Functions/Auth/IGraphAccessTokenProvider.cs` and `/home/adiazcan/github/todo-api/src/TodoApi.Functions/Auth/MsalGraphAccessTokenProvider.cs`
- [x] T008 [P] Implement the Graph client factory with timeout and retry policy in `/home/adiazcan/github/todo-api/src/TodoApi.Functions/Auth/GraphServiceClientFactory.cs` and `/home/adiazcan/github/todo-api/src/TodoApi.Functions/Program.cs`
- [x] T009 Create shared API error mapping and envelope helpers in `/home/adiazcan/github/todo-api/src/TodoApi.Functions/Services/TodoErrorMapper.cs` and `/home/adiazcan/github/todo-api/src/TodoApi.Functions/Services/ApiResponseFactory.cs`
- [x] T010 Create the bootstrap utility options and interactive consent flow skeleton in `/home/adiazcan/github/todo-api/tools/TodoApi.AuthBootstrap/AuthBootstrapOptions.cs` and `/home/adiazcan/github/todo-api/tools/TodoApi.AuthBootstrap/Program.cs`

**Checkpoint**: The solution can bind configuration, acquire Graph access tokens, construct a Graph client, and emit stable API envelopes.

---

## Phase 3: User Story 1 - Create Task In Specified List (Priority: P1) 🎯 MVP

**Goal**: Accept valid task text plus a valid list ID and create exactly one task in that Microsoft To Do list for the configured account.

**Independent Test**: Submit a request containing valid task text and a valid list ID and confirm that exactly one new task appears in that list with a success response.

### Tests for User Story 1

- [x] T011 [P] [US1] Add success contract coverage for `POST /api/tasks`, including created task ID and target list ID, in `/home/adiazcan/github/todo-api/tests/TodoApi.ContractTests/CreateTaskSuccessContractTests.cs`
- [x] T012 [P] [US1] Add integration coverage for successful task creation, whitespace trimming, and multiline or punctuation-preserving task text in `/home/adiazcan/github/todo-api/tests/TodoApi.IntegrationTests/CreateTaskSuccessTests.cs`
- [x] T013 [P] [US1] Add unit coverage for list validation and task creation orchestration in `/home/adiazcan/github/todo-api/tests/TodoApi.UnitTests/Services/TodoTaskServiceTests.cs`

### Implementation for User Story 1

- [x] T014 [P] [US1] Create normalized command and created-task models in `/home/adiazcan/github/todo-api/src/TodoApi.Functions/Services/NormalizedTaskCommand.cs` and `/home/adiazcan/github/todo-api/src/TodoApi.Functions/Services/CreatedTodoTask.cs`
- [x] T015 [P] [US1] Implement target-list lookup and task creation orchestration in `/home/adiazcan/github/todo-api/src/TodoApi.Functions/Services/ITodoTaskService.cs` and `/home/adiazcan/github/todo-api/src/TodoApi.Functions/Services/TodoTaskService.cs`
- [x] T016 [US1] Implement the happy-path `POST /api/tasks` HTTP trigger in `/home/adiazcan/github/todo-api/src/TodoApi.Functions/Functions/CreateTaskFunction.cs`
- [x] T017 [US1] Wire task creation services and Graph registrations in `/home/adiazcan/github/todo-api/src/TodoApi.Functions/Program.cs`

**Checkpoint**: User Story 1 creates one task in the caller-selected list and returns a success payload with task and list identifiers.

---

## Phase 4: User Story 2 - Receive Clear Outcome (Priority: P2)

**Goal**: Return explicit validation, auth, and upstream failure results so callers can distinguish correction-needed requests from retryable dependency failures.

**Independent Test**: Submit one valid request and one invalid or unreachable-account request, then confirm the responses clearly distinguish success from failure and explain the failure cause.

### Tests for User Story 2

- [ ] T018 [P] [US2] Add contract coverage for validation and dependency failure envelopes in `/home/adiazcan/github/todo-api/tests/TodoApi.ContractTests/CreateTaskFailureContractTests.cs`
- [ ] T019 [P] [US2] Add integration coverage for blank text, blank list ID, unauthorized access, and Graph failures in `/home/adiazcan/github/todo-api/tests/TodoApi.IntegrationTests/CreateTaskFailureTests.cs`
- [ ] T020 [P] [US2] Add unit coverage for request validation and error classification in `/home/adiazcan/github/todo-api/tests/TodoApi.UnitTests/Validation/CreateTaskRequestValidatorTests.cs` and `/home/adiazcan/github/todo-api/tests/TodoApi.UnitTests/Services/TodoErrorMapperTests.cs`

### Implementation for User Story 2

- [ ] T021 [P] [US2] Implement request validation and caller-correctable failure modeling in `/home/adiazcan/github/todo-api/src/TodoApi.Functions/Services/CreateTaskRequestValidator.cs` and `/home/adiazcan/github/todo-api/src/TodoApi.Functions/Services/ValidationResult.cs`
- [ ] T022 [P] [US2] Implement list-resolution, token-refresh, and Graph failure translation in `/home/adiazcan/github/todo-api/src/TodoApi.Functions/Services/TodoTaskService.cs` and `/home/adiazcan/github/todo-api/src/TodoApi.Functions/Services/TodoErrorMapper.cs`
- [ ] T023 [US2] Update `/home/adiazcan/github/todo-api/src/TodoApi.Functions/Functions/CreateTaskFunction.cs` to return definitive `400`, `401`, `502`, and `503` responses for failure paths
- [ ] T024 [US2] Add structured logging for validation, authentication, and upstream dependency failures in `/home/adiazcan/github/todo-api/src/TodoApi.Functions/Functions/CreateTaskFunction.cs` and `/home/adiazcan/github/todo-api/src/TodoApi.Functions/Services/TodoTaskService.cs`

**Checkpoint**: User Story 2 reports clear success or failure outcomes without false positives and without creating tasks on rejected requests.

---

## Phase 5: User Story 3 - Keep Requests Focused (Priority: P3)

**Goal**: Preserve a strict v1 request shape that accepts only `text` and `listId`, performs trimmed-only normalization, and creates exactly one task per successful request.

**Independent Test**: Submit a minimal valid request containing only task text and list ID and confirm that the system creates one task in the selected list without requiring any extra input.

### Tests for User Story 3

- [ ] T025 [P] [US3] Add contract coverage for the minimal request body and unknown-field rejection in `/home/adiazcan/github/todo-api/tests/TodoApi.ContractTests/CreateTaskMinimalPayloadContractTests.cs`
- [ ] T026 [P] [US3] Add integration coverage for minimal payload acceptance and exactly-one-create behavior in `/home/adiazcan/github/todo-api/tests/TodoApi.IntegrationTests/CreateTaskMinimalPayloadTests.cs`
- [ ] T027 [P] [US3] Add unit coverage for single-create execution guarantees in `/home/adiazcan/github/todo-api/tests/TodoApi.UnitTests/Services/TodoTaskServiceSingleCreateTests.cs`

### Implementation for User Story 3

- [ ] T028 [P] [US3] Enforce strict JSON payload handling and trimmed-only normalization in `/home/adiazcan/github/todo-api/src/TodoApi.Functions/Contracts/CreateTaskRequest.cs` and `/home/adiazcan/github/todo-api/src/TodoApi.Functions/Program.cs`
- [ ] T029 [P] [US3] Update `/home/adiazcan/github/todo-api/src/TodoApi.Functions/Services/TodoTaskService.cs` to validate the target list before issuing one Graph create call per successful request
- [ ] T030 [US3] Restrict the success payload to the published v1 fields in `/home/adiazcan/github/todo-api/src/TodoApi.Functions/Functions/CreateTaskFunction.cs` and `/home/adiazcan/github/todo-api/src/TodoApi.Functions/Contracts/CreateTaskSuccessResponse.cs`

**Checkpoint**: User Story 3 accepts the minimal v1 request shape, rejects unsupported input, and guarantees one created task per successful request.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Finish documentation, production hardening, and end-to-end validation across all stories.

- [ ] T031 [P] Update setup and deployment guidance in `/home/adiazcan/github/todo-api/README.md` and `/home/adiazcan/github/todo-api/specs/001-todo-task-api/quickstart.md`
- [ ] T032 [P] Add bootstrap utility usage and secret-installation guidance in `/home/adiazcan/github/todo-api/tools/TodoApi.AuthBootstrap/README.md`
- [ ] T033 [P] Harden hosting, timeout, and authentication defaults in `/home/adiazcan/github/todo-api/src/TodoApi.Functions/host.json` and `/home/adiazcan/github/todo-api/src/TodoApi.Functions/local.settings.json.example`
- [ ] T034 Document quickstart validation outcomes and deployment notes in `/home/adiazcan/github/todo-api/specs/001-todo-task-api/quickstart.md` and `/home/adiazcan/github/todo-api/README.md`
- [ ] T035 [P] Add Microsoft To Do boundary contract coverage for Graph request and response translation in `/home/adiazcan/github/todo-api/tests/TodoApi.ContractTests/GraphBoundaryContractTests.cs`
- [ ] T036 [P] Add Siri Shortcuts-compatible payload schema contract coverage for the published task-creation request in `/home/adiazcan/github/todo-api/tests/TodoApi.ContractTests/SiriPayloadContractTests.cs`
- [ ] T037 [P] Add performance and concurrency validation for the 5-second response target and 100-concurrent-request bursts in `/home/adiazcan/github/todo-api/tests/TodoApi.IntegrationTests/CreateTaskPerformanceTests.cs` and `/home/adiazcan/github/todo-api/specs/001-todo-task-api/quickstart.md`
- [ ] T038 [P] Validate the first-time integrator quickstart flow against the published request and response contract in `/home/adiazcan/github/todo-api/specs/001-todo-task-api/quickstart.md` and `/home/adiazcan/github/todo-api/README.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- Setup (Phase 1) has no dependencies and can start immediately.
- Foundational (Phase 2) depends on Setup and blocks all user story work.
- User Story 1 (Phase 3) depends on Foundational completion.
- User Story 2 (Phase 4) depends on Foundational completion and should be layered on the endpoint built in User Story 1.
- User Story 3 (Phase 5) depends on Foundational completion and should be layered after User Story 1 stabilizes the core request flow.
- Polish (Phase 6) depends on all selected user stories being complete.

### User Story Dependencies

- US1 is the MVP and has no dependency on other user stories.
- US2 reuses the US1 endpoint and service flow to add definitive failure handling.
- US3 reuses the US1 endpoint and service flow to lock down the minimal v1 contract and single-create guarantee.

### Within Each User Story

- Write contract, integration, and unit tests first and confirm they fail.
- Implement models and service abstractions before endpoint wiring.
- Complete the story end-to-end before moving to the next priority when working sequentially.

### Parallel Opportunities

- T003 and T004 can run in parallel during Setup.
- T006, T007, and T008 can run in parallel during Foundational work.
- T011, T012, and T013 can run in parallel for US1 test authoring.
- T014 and T015 can run in parallel for US1 implementation.
- T018, T019, and T020 can run in parallel for US2 test authoring.
- T021 and T022 can run in parallel for US2 implementation.
- T025, T026, and T027 can run in parallel for US3 test authoring.
- T028 and T029 can run in parallel for US3 implementation.
- T031, T032, T033, T035, T036, T037, and T038 can run in parallel during Polish.

---

## Parallel Example: User Story 1

```bash
# Write the User Story 1 tests together
Task: "T011 Add success contract coverage for POST /api/tasks"
Task: "T012 Add integration coverage for successful task creation and whitespace trimming"
Task: "T013 Add unit coverage for list validation and task creation orchestration"

# Then implement the story models and service in parallel
Task: "T014 Create normalized command and created-task models"
Task: "T015 Implement target-list lookup and task creation orchestration"
```

## Parallel Example: User Story 2

```bash
# Write the User Story 2 failure-path tests together
Task: "T018 Add contract coverage for validation and dependency failure envelopes"
Task: "T019 Add integration coverage for blank text, blank list ID, unauthorized access, and Graph failures"
Task: "T020 Add unit coverage for request validation and error classification"

# Then implement validation and error translation in parallel
Task: "T021 Implement request validation and caller-correctable failure modeling"
Task: "T022 Implement list-resolution, token-refresh, and Graph failure translation"
```

## Parallel Example: User Story 3

```bash
# Write the User Story 3 contract, integration, and unit tests together
Task: "T025 Add contract coverage for the minimal request body and unknown-field rejection"
Task: "T026 Add integration coverage for minimal payload acceptance and exactly-one-create behavior"
Task: "T027 Add unit coverage for single-create execution guarantees"

# Then implement payload strictness and single-create enforcement in parallel
Task: "T028 Enforce strict JSON payload handling and trimmed-only normalization"
Task: "T029 Update TodoTaskService to validate the target list before issuing one Graph create call"
```

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup.
2. Complete Phase 2: Foundational.
3. Complete Phase 3: User Story 1.
4. Validate `POST /api/tasks` happy-path behavior against the quickstart scenario.

### Incremental Delivery

1. Ship the core create-task flow with User Story 1.
2. Add explicit failure handling and caller guidance with User Story 2.
3. Lock the request surface and exactly-once behavior with User Story 3.
4. Finish with documentation, hosting hardening, and quickstart validation.

### Suggested MVP Scope

- Phase 1: Setup
- Phase 2: Foundational
- Phase 3: User Story 1

## Notes

- Tasks marked `[P]` touch separate files and can be worked in parallel after their dependencies are complete.
- Each user story phase is scoped to remain independently testable.
- The feature’s implementation order should stay close to `US1 -> US2 -> US3` because all stories extend the same public endpoint.