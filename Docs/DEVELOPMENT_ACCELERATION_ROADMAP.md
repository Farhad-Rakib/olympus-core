# Development Acceleration Roadmap

## Objective

Reduce the time from a feature request to a reviewable release while preserving the existing Clean Architecture backend and React feature structure.

## Current Baseline

- Backend: .NET 8, versioned ASP.NET API, Clean Architecture, EF Core or Dapper selection, PostgreSQL or SQL Server selection, Redis support, Swagger, health checks, SignalR, RBAC, and audit functionality.
- Frontend: React, TypeScript, Vite, Tailwind CSS, Zustand, TanStack Query, React Hook Form, Zod, Axios, reusable tables/forms, route guards, mock services, and feature folders.
- Existing strengths: shared API repository, service interfaces, configuration-driven forms/tables, seeded administration data, and deployment documentation.
- Current gaps: no frontend test runner, no visible dedicated backend test projects, likely duplicated DTO/service/page wiring for each feature, and manual API/frontend contract synchronization.

## Highest-Value Enhancements

### 1. Create a Standard Feature Generator

Add a documented or CLI-based generator that creates the files for one feature:

- Domain model and DTOs.
- Backend command/query, validator, handler, repository contract, and controller.
- Frontend service interface, API implementation, React Query hooks, route, page, form, table, and permission constants.
- Migration and seed-data placeholders.

The generator should ask for the entity name, fields, CRUD operations, permissions, menu location, and database strategy. This is the fastest path to consistent feature delivery.

### 2. Make OpenAPI the Shared Contract

Generate or validate TypeScript API types from the backend OpenAPI document. Add a CI check that fails when a backend contract changes without the frontend client/types being updated.

Benefits:

- Fewer hand-written DTO mismatches.
- Safer refactoring of response envelopes and validation errors.
- Faster frontend service implementation.
- Easier external integration and SDK generation.

### 3. Standardize API Errors and Frontend Error Handling

Define one problem-details shape for validation, authorization, not-found, conflict, and unexpected errors. Map that shape in the Axios repository and display field-level errors in forms automatically.

This removes repeated error parsing from each feature and makes support logs easier to interpret.

### 4. Centralize Permissions and Menu Metadata

Use one source of truth for:

- Permission key.
- Human-readable label.
- Description.
- Required role or policy.
- Menu visibility.
- API route mapping.

Generate backend policies, seed permissions, frontend route guards, and menu definitions from that metadata where practical. Keep server-side authorization authoritative; frontend visibility is only a usability feature.

### 5. Add CI Quality Gates

A pull request should run, at minimum:

- Backend restore, build, and format check.
- Frontend install, lint, typecheck, and production build.
- OpenAPI compatibility check.
- Migration validation against a clean database.
- Dependency and secret scanning.

Publish build logs and artifacts so failures are diagnosable without reproducing locally.

## Medium-Term Enhancements

### Developer Experience

- Add VS Code tasks for API, frontend, dependencies, and migrations.
- Add commit hooks for formatting and linting.
- Add a pull request template with feature, permission, migration, and rollback checkboxes.
- Add architecture decision records for cross-cutting choices.
- Add a local seeded dataset command that can reset safely.

### Observability

- Use structured correlation IDs from browser request to API log.
- Add request timing, database timing, cache hit/miss, and SignalR connection metrics.
- Add an error reporting integration with environment and release metadata.
- Add a safe admin diagnostics page that links health, dependency, cache, and recent error status without exposing secrets.

### Frontend Productivity

- Expand the existing generic DataTable and DynamicForm into schema-driven components.
- Add a consistent query-key factory and mutation invalidation policy.
- Add optimistic updates only for operations where rollback is well defined.
- Add visual regression checks for shared layout and form components.

### Backend Productivity

- Add reusable application behaviors for validation, logging, authorization, transactions, and idempotency.
- Add pagination, filtering, sorting, and projection conventions shared by all list endpoints.
- Add migration review and backward-compatible deployment rules.

## Suggested Sequence

| Phase | Deliverable | Success measure |
|---|---|---|
| 1 | OpenAPI types and standard errors | Contract mismatches fail in CI instead of during manual testing. |
| 2 | Centralized permissions and menu metadata | Permissions, policies, routes and menus come from one definition. |
| 3 | Feature generator | A standard CRUD feature is scaffolded in minutes. |
| 4 | CI quality gates | Every merge receives repeatable build and workflow feedback. |

## Definition of Done for a New Feature

- API contract, authorization policy, validation, persistence, and audit behavior are defined.
- Frontend route, loading state, empty state, error state, form validation, and responsive behavior are implemented.
- Permissions and menu visibility are checked for both allowed and denied users.
- Documentation, migration notes, seed data, and rollback considerations are updated.
