# Authorization Overview

This project uses a permission-based authorization model with role support.

Key concepts

- Permissions: Named constants defined in `ProjectNamePlaceholder.Application.Security.Permissions` (e.g., `site-settings.read`, `menus.create`). Policies are registered for these permissions at startup.
- Claims: When a user authenticates, their JWT includes a `permission` claim for each allowed permission.
- Policy enforcement: `PermissionRequirement` and `PermissionAuthorizationHandler` check the user's `permission` claims against the required permission for an endpoint.
- Self-or-permission: `SelfOrPermissionRequirement` allows users to act on their own resources or, with permission, on others.
- SuperAdmin bypass: The authorization handlers short-circuit and succeed if the user is in the `SuperAdmin` role. This allows a super administrator to operate without explicit permission grants.
- Menu permissions: `Menu.RequiredPermission` stores the name of a permission required to view a menu entry. A `MenuPermissionValidator` runs during bootstrap to verify required permissions exist in the permissions table.

Assigning permissions

- Permissions can be assigned to roles via the seeders or through the admin UI (see Docs/ASSIGN_ROLE_PERMISSION.md).
- The RBAC seeders create basic roles and permissions. If you want explicit grants for a role, update `RbacSeeder` or use the admin endpoints.

Where to look in the code

- Permission constants: [ProjectNamePlaceholder.Application/Security/Permissions.cs](ProjectNamePlaceholder.Application/Security/Permissions.cs)
- Authorization handlers: [ProjectNamePlaceholder.Api/Authorization/PermissionAuthorizationHandler.cs](ProjectNamePlaceholder.Api/Authorization/PermissionAuthorizationHandler.cs)
- Menu validator: [ProjectNamePlaceholder.Api/Startup/MenuPermissionValidator.cs](ProjectNamePlaceholder.Api/Startup/MenuPermissionValidator.cs)
- Seeder bootstrap: [ProjectNamePlaceholder.Persistence/Seeding/DefaultSiteSettingsSeeder.cs](ProjectNamePlaceholder.Persistence/Seeding/DefaultSiteSettingsSeeder.cs)

Behavior notes

- Validators run on startup and will warn or fail depending on configuration (`Validation:MenuPermissionStrict`).
- The SuperAdmin bypass simplifies administration but rely on it only if you control who is assigned that role.
