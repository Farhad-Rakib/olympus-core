**Assigning Roles and Permissions — Guide

This document shows API examples and explains how the backend handles roles and permissions.

**Quick Rationale: `RequiredPermission` is a string**
- Human-readable stable identifier: permission names (e.g. `reports.read`) are stable across environments.
- Matches claims: user permission claims are strings, so checking `RequiredPermission` against claims is a straight string compare (no join needed).
- Simpler seeding and configuration: seeders and admin UI can create menu items without resolving DB ids.
- Tradeoffs: no DB foreign-key safety (risk of typos). Mitigate by seeding/validation and tests.

**Key API endpoints (v1)**
- Create a role: `POST /api/v1/roles` (policy: `roles.create`)
- Create a permission: `POST /api/v1/permissions` (policy: `permissions.create`)
- Add permission to role: `POST /api/v1/roles/{roleId}/permissions/{permissionId}` (policy: `role-permissions.create`)
- Add role to user: `POST /api/v1/users/{userId}/roles/{roleId}` (policy: `user-roles.create`)
- Get menu for current user: `GET /api/menu` (authenticated)
- Manage menus: `POST /api/menu` (policy: `menus.create`) etc.

**Example: create a permission, grant it to a role, assign role to user (curl)**

1) Create permission

```bash
curl -X POST "https://example.local/api/v1/permissions" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"name":"reports.read","displayName":"Read Reports"}'
```

Response: permission object with `id`.

2) Create role (if not present)

```bash
curl -X POST "https://example.local/api/v1/roles" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"name":"Reporter"}'
```

Response: role object with `id`.

3) Grant permission to role (use ids returned earlier)

```bash
curl -X POST "https://example.local/api/v1/roles/123/permissions/456" \
  -H "Authorization: Bearer $ADMIN_TOKEN"
```

4) Assign role to a user

```bash
curl -X POST "https://example.local/api/v1/users/789/roles/123" \
  -H "Authorization: Bearer $ADMIN_TOKEN"
```

Now user `789` will receive claims for the role's permissions when they authenticate.

**How the backend applies this at runtime**
- Authentication issues a JWT with claims for user id and granted permissions. Permissions are added as claims of type `permission` (string values).
- Menu rendering: `GET /api/menu` calls `IMenuService.GetMenuForUserAsync`, which loads menus and filters them by comparing each menu's `RequiredPermission` (string) against the set of `permission` claims the user has.
- Authorization on endpoints: policies are registered from `Permissions.All` at startup and mapped to a `PermissionRequirement` / `PermissionAuthorizationHandler` that checks whether the current user has the permission claim.
- Role/permission assignment: adding a permission to a role updates DB role-permission mappings; next time the user re-authenticates (or token refresh), their permission claims will include the new permission.

**Notes & best practices**
- SuperAdmin: seeders typically grant SuperAdmin every permission so menu items protected by any permission are visible to SuperAdmin.
- To avoid stale/typo issues: add a small startup validation that logs (or fails in dev) when a `Menu.RequiredPermission` doesn't exist in the permissions table.
- If you prefer referential integrity: store `PermissionId` on `Menu` and join at render-time; you'll need to map permission rows to strings for claim checks.

**Files to inspect**
- Role endpoints: [ProjectNamePlaceholder.Api/Controllers/RoleController.cs](ProjectNamePlaceholder.Api/Controllers/RoleController.cs#L1)
- User role endpoints: [ProjectNamePlaceholder.Api/Controllers/UsersController.cs](ProjectNamePlaceholder.Api/Controllers/UsersController.cs#L1)
- Menu service/filtering: [ProjectNamePlaceholder.Application/Menu/MenuService.cs](ProjectNamePlaceholder.Application/Menu/MenuService.cs#L1)
- Permission constants: [ProjectNamePlaceholder.Application/Security/Permissions.cs](ProjectNamePlaceholder.Application/Security/Permissions.cs#L1)

If you want, I can add:
- a) an initial seeder entry that grants `reports.read` to a specific role in `RbacSeeder.cs`, or
- b) a small startup validation that warns when menus reference non-existent permission names.

