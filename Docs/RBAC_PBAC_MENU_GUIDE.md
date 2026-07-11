# RBAC, PBAC, and Menu Generation Guide

This guide explains how role-based access control, permission-based access control, and menu generation work together in this codebase.

## Concepts

- RBAC controls what a user can do through roles.
- PBAC controls access with explicit permissions attached to roles and checked by policies.
- Menu generation controls what a user can see in the UI based on the permissions they have.

## Data flow

1. A user signs in and receives claims that include their permissions.
2. Roles are assigned permissions.
3. Permissions are synced into the policy system at startup.
4. Menu items declare a required permission string.
5. The menu service filters visible menu items using the user’s permission claims.

## RBAC implementation

Roles are the primary grouping mechanism for access control.

- Define or update roles in the roles feature.
- Assign permissions to roles using the role-permission flow.
- Seed default roles and permissions during deployment or bootstrap.
- Keep role names stable and use permissions for fine-grained control.

Recommended pattern:

- Use roles for business grouping such as Admin, Manager, and Support.
- Use permissions for action-level checks such as `users.read`, `roles.update`, or `site-settings.delete`.

## PBAC implementation

PBAC in this project is implemented with named permission policies.

- Add the permission constant in [Permissions.cs](ProjectNamePlaceholder.Application/Security/Permissions.cs).
- Register the policy during startup in [Program.cs](ProjectNamePlaceholder.Api/Program.cs).
- Protect controller actions with `[Authorize(Policy = Permissions.SomePermission)]`.
- Sync permissions into the database so the menu and role assignment screens can reference them.

Typical examples:

- `users.read` for listing users.
- `menus.create` for creating menu items.
- `system.cache.flush` for flushing Redis cache keys.

## Menu generation

Menu generation is driven by the `Menu` entity in the domain layer and the menu service in the application layer.

- The domain entity is named `Menu`.
- The menu record stores `Title`, `Url`, `Icon`, `RequiredPermission`, and `ParentMenuId`.
- The menu service loads menu records, builds the tree, and filters items by permission claims.
- The default seeder creates the application menu tree on startup.

Menu rules:

- If `RequiredPermission` is empty, the item is visible to any authenticated user.
- If `RequiredPermission` has a value, the user must have that permission claim to see the item.
- Parent-child menu relationships are built with `ParentMenuId`.

## Adding a new protected menu item

1. Add the permission constant to [Permissions.cs](ProjectNamePlaceholder.Application/Security/Permissions.cs).
2. Add the policy usage on the target API endpoint.
3. Seed or create the menu item with `RequiredPermission` set to that permission string.
4. Ensure the permission is assigned to the correct roles.
5. Verify the item appears for authorized users and stays hidden for others.

Example:

```csharp
await UpsertMenuAsync(dbContext, "Audit Logs", "/audit-logs", "shield", Permissions.ReportsRead, configuration);
```

## Startup validation

The app already validates menu permissions on startup.

- `MenuPermissionValidator` checks that each menu permission exists in the permissions table.
- In strict mode, startup fails on missing permissions.
- In non-strict mode, the app logs warnings so you can fix the seed data later.

## Caching and admin operations

Menu, role, permission, and site settings data may be cached through `IDistributedCache`.

- Use Redis in production for shared cache state across nodes.
- Invalidate caches after create, update, and delete operations.
- Use the system cache endpoints to inspect or flush cache entries when debugging.

## Deployment checklist

1. Seed permissions and roles.
2. Seed default menu items.
3. Confirm permission sync runs at startup.
4. Confirm menu permission validation passes.
5. Enable Redis if the environment requires distributed caching.
6. Verify protected menu items appear only for authorized users.

## Implementation references

- Permission constants: [ProjectNamePlaceholder.Application/Security/Permissions.cs](ProjectNamePlaceholder.Application/Security/Permissions.cs)
- Menu service: [ProjectNamePlaceholder.Application/Menu/MenuService.cs](ProjectNamePlaceholder.Application/Menu/MenuService.cs)
- Menu controller: [ProjectNamePlaceholder.Api/Controllers/MenuController.cs](ProjectNamePlaceholder.Api/Controllers/MenuController.cs)
- Menu validator: [ProjectNamePlaceholder.Api/Startup/MenuPermissionValidator.cs](ProjectNamePlaceholder.Api/Startup/MenuPermissionValidator.cs)
- Default menu seeder: [ProjectNamePlaceholder.Persistence/Seeding/DefaultMenuSeeder.cs](ProjectNamePlaceholder.Persistence/Seeding/DefaultMenuSeeder.cs)