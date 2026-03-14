<!--
## Sync Impact Report

**Version change**: (new) → 1.0.0

**Principles added**:
- I. Code Quality (new)
- II. Testing Standards (new)
- III. User Experience Consistency (new)
- IV. Performance Requirements (new)

**Sections added**:
- Security Requirements
- Development Workflow

**Sections removed**: none

**Templates checked**:
- ✅ `.specify/templates/plan-template.md` — Constitution Check gates align with principles
- ✅ `.specify/templates/spec-template.md` — Success Criteria section aligned with performance and UX principles
- ✅ `.specify/templates/tasks-template.md` — Task phases reflect testing standards and quality gates
- ✅ `.specify/templates/constitution-template.md` — source template unchanged (no edits required)

**Deferred TODOs**: none
-->

# todo-api Constitution

## Core Principles

### I. Code Quality (NON-NEGOTIABLE)

All code merged into the main branch MUST meet the following baseline standards:

- Every module, function, and API endpoint MUST have a single, clearly stated responsibility (SRP).
- Code MUST be reviewed by at least one peer before merging; self-merges are prohibited.
- Linting and static analysis MUST pass with zero errors (warnings may be waived with documented justification).
- Dead code, commented-out blocks, and unchecked `TODO` markers older than one sprint MUST NOT be merged.
- Dependencies MUST be pinned to explicit versions; floating ranges are prohibited in production manifests.

**Rationale**: Consistent code quality reduces cognitive overhead during Siri-integration debugging and
ensures the API surface remains predictable as Microsoft To Do's underlying schema evolves.

### II. Testing Standards (NON-NEGOTIABLE)

Automated tests are a first-class deliverable, not an afterthought:

- Unit tests MUST cover all business logic functions with a minimum of 80% line coverage.
- Every public API endpoint MUST have at least one integration test exercising the happy path and one
  exercising a representative error path.
- Tests MUST be written (and confirmed failing) before implementation begins (Red-Green-Refactor).
- Contract tests MUST be maintained for all external service boundaries (Microsoft To Do API, Siri
  Shortcuts payload schema).
- Flaky tests MUST be quarantined or fixed within the same sprint they are identified.

**Rationale**: The Siri ↔ API ↔ Microsoft To Do chain has multiple failure surfaces; comprehensive
automated tests are the primary safeguard against silent regressions.

## Development Workflow

- All new work MUST be developed on a feature branch; direct commits to `main` are prohibited.
- Every feature MUST have a corresponding spec before implementation begins.
- A Constitution Check MUST be completed in `plan.md` before Phase 1 design work starts, and again
  after design is finalized.
- Pull requests MUST reference the spec and include a testing summary before they are eligible for review.
- The build pipeline MUST enforce linting, unit tests, and integration tests as blocking gates; a
  failing gate MUST prevent merge.

## Governance

This constitution supersedes all other development practices. Any practice not covered here defaults
to current industry best practices and MUST be raised for explicit inclusion in the next amendment.

- Amendments require a written proposal, at least one reviewer approval, and documentation of any
  migration plan for affected existing code.
- The version follows semantic versioning: MAJOR for backward-incompatible governance changes or
  principle removals; MINOR for new principles or materially expanded guidance; PATCH for
  clarifications and wording fixes.
- Constitution compliance MUST be verified in every pull request as part of the "Constitution Check"
  section in `plan.md`.
- Complexity violations (deviations from any principle) MUST be recorded in the Complexity Tracking
  table in `plan.md` with explicit justification.

**Version**: 1.0.0 | **Ratified**: 2026-03-14 | **Last Amended**: 2026-03-14
