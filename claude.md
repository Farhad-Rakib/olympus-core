# Claude Notes

## Project Snapshot

This repository is a layered ASP.NET Core API built around `ProjectNamePlaceholder` namespaces.

- `ProjectNamePlaceholder.Api` hosts HTTP endpoints, middleware, Swagger, SignalR, auth, caching, and startup bootstrap.
- `ProjectNamePlaceholder.Application` contains use-case services and FluentValidation validators.
- `ProjectNamePlaceholder.Infrastructure` contains JWT, password hashing, email, and other external integrations.
- `ProjectNamePlaceholder.Persistence` contains the database context, repositories, seeding, and database bootstrap logic.
- `ProjectNamePlaceholder.Domain` contains entities, enums, common models, and value objects.

## Runtime And Composition

- Target framework is .NET 8.
- Authentication uses JWT bearer tokens.
- Authorization is policy-based and includes permission requirements plus self-or-permission checks.
- The API exposes Swagger in development and enables SignalR notifications.
- CORS allows `http://localhost:5173` in development.
- Caching can use Redis when `Caching:UseRedis` is enabled; otherwise memory cache is used.
- Database provider is selected with `Database:Provider` and supports `postgres` or `sqlserver`.
- Startup performs database bootstrap, menu-permission validation, and permission sync.

## Important Configuration Keys

- `ConnectionStrings:PostgresConnection`
- `ConnectionStrings:SqlServerConnection`
- `ConnectionStrings:Redis`
- `Database:Provider`
- `Caching:UseRedis`
- `Caching:RedisInstanceName`
- `Caching:PermissionsTtlMinutes`
- `Caching:MenusTtlMinutes`
- `Caching:RolesTtlMinutes`
- `Caching:RolePermissionsTtlMinutes`
- `Jwt:Issuer`
- `Jwt:Audience`
- `Jwt:SecretKey`
- `Jwt:ExpiryMinutes`
- `Jwt:RefreshTokenExpiryDays`
- `Smtp:*`
- `Serilog:*`

## Editing Guidance

- Keep changes consistent with the existing layered architecture.
- Prefer updating the owning layer instead of pushing logic into `Api`.
- When touching auth, check `Api/Authorization`, `Infrastructure/Authentication`, and `Application/Security` together.
- When touching persistence, verify the selected database provider and the matching connection string.
- Avoid renaming the placeholder project names unless the whole solution is being rebranded.