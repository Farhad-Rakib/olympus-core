# Olympus End-to-End Test Cases

## Purpose

This document is the QA checklist for the Olympus application across the React frontend and .NET backend. It covers the current application surface and can be used for manual regression testing until automated test projects are added.

The backend API uses versioned routes such as `/api/v1/auth/login`. The frontend is the React application in `olympus-react` and the API is the .NET application in `olympus-core`.

## Test Environments

| Environment | Frontend | API | Supporting services |
|---|---|---|---|
| Local | `http://localhost:5173` | `http://localhost:5000` or configured HTTPS port | PostgreSQL or SQL Server, Redis when enabled, MailHog for email |
| CI | Built frontend artifact | Test API process/container | Ephemeral database and service containers |
| Staging | Deployed frontend | Deployed API | Staging database, Redis, SMTP, logging |

Use a fresh database for destructive or seed-sensitive cases. Never use production credentials in this checklist.

## Preconditions and Test Data

- Start the backend with the required database configuration.
- Start the frontend with `npm run dev` or use the deployed build.
- Apply migrations and seed data.
- Confirm the health endpoint responds: `GET /health`.
- Confirm Swagger is enabled in development.
- Use the seeded administrator documented in the root README, or create dedicated test users.
- Create these test accounts:
  - `admin-test`: SuperAdmin or equivalent administrative role.
  - `manager-test`: limited role with user read permission only.
  - `user-test`: no administrative permissions.
  - `disabled-test`: disabled or otherwise invalid account, if supported by the environment.
- Keep known IDs for one test user, one role, one permission, one menu item, and one site setting.

## Smoke and Availability Tests

| ID | Area | Steps | Expected result |
|---|---|---|---|
| SMK-001 | API health | Call `GET /health`. | HTTP 200 and a healthy response. |
| SMK-002 | API startup | Start the API with the configured database. | Startup completes without migration, dependency, or menu-permission validation errors. |
| SMK-003 | Frontend load | Open the frontend URL in a clean browser session. | Login page renders without console errors or failed static assets. |
| SMK-004 | API documentation | Open Swagger in development. | Versioned API operations are listed and can be authorized with a bearer token. |
| SMK-005 | Production safety | Run the production configuration. | Swagger is disabled unless explicitly enabled and secrets are supplied through deployment configuration. |

## Authentication and Session Tests

| ID | Steps | Expected result |
|---|---|---|
| AUTH-001 | Log in with valid credentials. | User is redirected to the dashboard; access and refresh session state are established. |
| AUTH-002 | Log in with an invalid password. | Login is rejected with a safe error; no token or privileged data is stored. |
| AUTH-003 | Submit an empty or malformed login form. | Client-side validation prevents submission or API returns a validation error. |
| AUTH-004 | Refresh the browser after login. | The session is restored when the refresh token is valid. |
| AUTH-005 | Expire or invalidate the access token, then call a protected page. | The client refreshes once and retries safely, or redirects to login when refresh fails. |
| AUTH-006 | Log out. | Local session data is cleared, refresh is revoked where supported, and protected routes redirect to login. |
| AUTH-007 | Request a password reset for a known email. | A non-sensitive success response is shown and the test email appears in MailHog or the configured test mailbox. |
| AUTH-008 | Use an invalid, expired, or reused reset token. | Reset is rejected and the password is not changed. |
| AUTH-009 | Change password while authenticated. | The password changes, the old password fails, and the new password succeeds. |
| AUTH-010 | Attempt to access a protected API without credentials. | API returns HTTP 401. |

Relevant API operations include `POST /api/v1/auth/login`, `register`, `refresh`, `revoke-refresh`, `forgot-password`, `reset-password`, and `change-password`.

## Authorization and Navigation Tests

| ID | Steps | Expected result |
|---|---|---|
| RBAC-001 | Sign in as `admin-test`. | Administrative navigation and permitted pages are visible. |
| RBAC-002 | Sign in as `manager-test`. | Only assigned menu items are visible; restricted routes return the frontend 403 page or API HTTP 403. |
| RBAC-003 | Sign in as `user-test` and open an admin URL directly. | Direct navigation does not bypass authorization. |
| RBAC-004 | Assign a permission to a role, then assign the role to a user. | The user receives access after a fresh token/session where required. |
| RBAC-005 | Remove a permission or role. | The user loses access after authorization data and token claims are refreshed. |
| RBAC-006 | Verify a SuperAdmin action. | SuperAdmin bypass works only for the intended administrative authorization path. |
| RBAC-007 | Configure a menu item with a missing permission. | Startup validation reports the invalid permission according to strict/non-strict configuration. |
| RBAC-008 | Call a protected endpoint with a valid token but insufficient permission. | API returns HTTP 403 without exposing protected data. |

## Users, Roles, Permissions, and Menus

| ID | Steps | Expected result |
|---|---|---|
| ADM-001 | Open the users page and load the list. | Data table loads with correct loading, empty, error, pagination, and search states. |
| ADM-002 | View a user and update the user's own profile. | Valid profile changes persist and are shown after reload. |
| ADM-003 | Attempt to edit another user's profile without permission. | Operation is denied; self-or-permission behavior is enforced. |
| ADM-004 | Create, edit, and delete a role. | CRUD succeeds with valid data and rejects duplicate/invalid data. |
| ADM-005 | Create, edit, and delete a permission where allowed. | Permission CRUD persists and authorization uses the resulting permission. |
| ADM-006 | Assign and remove role permissions. | The role permission list and effective user access update correctly. |
| ADM-007 | Assign and remove user roles. | The user role list updates and access changes after re-authentication if claims are token-based. |
| ADM-008 | Create, edit, reorder, and delete a menu item. | Menu data persists and the frontend navigation reflects the permitted structure. |
| ADM-009 | Load menu as two users with different permissions. | Each user receives only allowed menu entries. |
| ADM-010 | Submit duplicate, missing, oversized, or invalid fields. | UI validation is clear and API validation returns a consistent error shape. |

## Settings, Profile, Preferences, and Notifications

| ID | Steps | Expected result |
|---|---|---|
| CFG-001 | Load general, security, and notification settings pages. | Settings render from the API or configured mock source without layout errors. |
| CFG-002 | Update a site setting and reload another browser session. | The new value is persisted and visible to the second session. |
| CFG-003 | Add, update, and delete a site setting. | CRUD succeeds and invalid keys/values are rejected appropriately. |
| CFG-004 | Load and update a palette setting. | Palette data persists and the selected theme is applied consistently. |
| CFG-005 | Change theme, sidebar, and preference options. | Preferences persist across reload and apply without visual regressions. |
| CFG-006 | Open the profile page and update profile data. | The header/profile view reflects the saved data. |
| CFG-007 | Receive a notification through REST. | Notification appears in the notification center and unread state is correct. |
| CFG-008 | Mark a notification read and delete it. | State changes persist after reload. |
| CFG-009 | Connect to the SignalR notification hub with a valid token. | The client connects and displays a received notification in real time. |
| CFG-010 | Connect to SignalR with an invalid or expired token. | Connection is rejected or re-authentication is requested without crashing the UI. |

## Reports, Dashboard, Activity, and System Operations

| ID | Steps | Expected result |
|---|---|---|
| OPS-001 | Open the dashboard with normal, empty, and slow API responses. | Metrics and loading/empty/error states are usable and do not shift unpredictably. |
| OPS-002 | Open reports and change filters/date ranges. | Charts and totals update consistently with the selected filters. |
| OPS-003 | Open the activity log as an authorized user. | Audit entries load with correct ordering and details. |
| OPS-004 | Attempt activity log access without permission. | Access is denied without leaking audit data. |
| OPS-005 | Call the endpoint inventory operation as an administrator. | Registered endpoints are returned; unauthorized users are denied. |
| OPS-006 | Inspect and flush local/distributed cache as an administrator. | Keys are listed and flush operations produce the expected invalidation. |

## Validation, Error, and Security Tests

- Verify all protected endpoints return 401 without a token and 403 with insufficient permissions.
- Verify malformed JSON, missing required fields, invalid IDs, duplicate records, and unsupported HTTP methods return consistent errors.
- Verify error responses do not expose stack traces, connection strings, JWT secrets, or personal data.
- Verify CORS, cookie, `SameSite`, `Secure`, and `HttpOnly` behavior in the deployed environment.
- Verify refresh-token rotation and revocation prevent reuse.
- Verify rate limiting or abuse controls for login and password reset when enabled.
- Verify keyboard navigation, focus order, form labels, contrast, responsive layout, and usable error messages at mobile and desktop widths.
- Verify browser refresh, back navigation, deep links, and expired sessions do not expose protected screens.

## Regression and Release Gates

Before merging backend changes:

1. `dotnet restore`.
2. `dotnet build`.
3. Run backend unit/integration tests when the test projects exist.
4. Start the API and execute the smoke, authentication, and authorization cases.
5. Confirm database migrations and seeders work against a clean database.

Before merging frontend changes:

1. `npm install`.
2. `npm run lint`.
3. `npm run typecheck`.
4. `npm run build`.
5. Execute the login, navigation, changed feature, responsive, and browser-console checks.

Before release:

- Run the full checklist against staging.
- Run API contract tests against the deployed API.
- Run browser E2E tests against the deployed frontend and API.
- Confirm health checks, logs, email delivery through the test route, and rollback readiness.

## Automation Backlog

The current React package does not include a test runner, and the repository does not show dedicated .NET test projects. Add automation in this order:

1. Backend unit tests for authorization handlers, validators, token services, and application handlers.
2. Backend integration tests using `WebApplicationFactory` and an isolated database.
3. Frontend unit/component tests with Vitest and Testing Library.
4. API contract tests generated or validated from OpenAPI.
5. Playwright browser tests for the smoke, authentication, RBAC, CRUD, and responsive cases.
6. CI reports for test results, coverage, screenshots, and traces.
