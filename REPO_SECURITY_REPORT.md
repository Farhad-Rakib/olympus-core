# Repository Problems And Vulnerabilities

Checked repositories:

- Backend: `/Users/farhadrakib/Personal Projects/olympusCore/src`
- Frontend: `/Users/farhadrakib/Personal Projects/olympus-react`

## Backend Findings

1. Hardcoded secrets and credentials are committed in [ProjectNamePlaceholder.Api/appsettings.json](ProjectNamePlaceholder.Api/appsettings.json#L8).
   - `PostgresConnection` uses `Username=postgres;Password=postgres`.
   - `SqlServerConnection` uses `User Id=sa;Password=YourStrong@Passw0rd`.
   - `Jwt:SecretKey` is a placeholder string instead of a real secret.
   - `Smtp:Password` is also stored in plain text.
   - Risk: accidental deployment with known credentials, easy credential reuse, and exposed JWT signing keys.

2. Host and environment defaults are too permissive in [ProjectNamePlaceholder.Api/appsettings.json](ProjectNamePlaceholder.Api/appsettings.json#L53).
   - `AllowedHosts` is set to `*`.
   - Risk: if the same configuration is used outside local development, the API will accept any host header.

## Frontend Findings

1. `npm audit --omit=dev` reports 6 vulnerabilities in the frontend dependency tree: 5 high and 1 moderate.
   - Direct high-severity dependencies include [package.json](../../olympus-react/package.json#L17) (`axios`) and [package.json](../../olympus-react/package.json#L22) (`react-router-dom`).
   - `postcss` is also present as a direct dependency in [package.json](../../olympus-react/package.json#L37) and is flagged by audit.
   - Notable transitive findings include `react-router`, `form-data`, and `ws`.
   - Risk: SSRF, prototype-pollution, open-redirect, XSS, and DoS exposure through shipped client dependencies.

2. The frontend README ships demo credentials in [olympus-react/README.md](../../olympus-react/README.md#L27).
   - Example accounts include `admin@example.com / admin123` and similar weak defaults.
   - Risk: these credentials are trivial to reuse if mock authentication or demo data is ever exposed beyond local testing.

## Recommended Fixes

- Move backend secrets to environment variables or a secret store and fail startup when placeholder values are detected.
- Replace all default database, SMTP, and JWT values before any non-local deployment.
- Restrict `AllowedHosts` to known production hostnames.
- Update frontend dependencies to patched versions, starting with `axios`, `react-router-dom`, and `postcss`.
- Remove or clearly isolate demo credentials so they cannot be confused with production authentication.

## Notes

- The backend codebase is layered and uses `Api`, `Application`, `Infrastructure`, `Persistence`, and `Domain` projects.
- The frontend is a Vite + React admin starter with a mock-driven architecture, so audit findings are mostly dependency-related rather than from unsafe DOM APIs.