Adding a New Menu Item and Required Permission

Follow these steps to add a new menu item and protect it with a permission.

1) Choose a permission name
- Pick a permission string (e.g. `reports.read`).

2) Register the permission
- Open `ProjectNamePlaceholder.Application/Security/Permissions.cs` and add a constant (e.g. `public const string ReportsRead = "reports.read";`) and add it to the `All` array. The application automatically registers policies from the `All` array at startup.

3) Grant the permission to roles
- Update `ProjectNamePlaceholder.Persistence/Seeding/RbacSeeder.cs` (or the Dapper seeder) to grant the new permission to appropriate roles (for example `SuperAdmin` already receives all permissions). Re-run the seeder or restart the app so the permission is present in the DB.

4) Add the menu entry
- Option A: Database seeder (recommended for initial/default menus)
  - Edit `ProjectNamePlaceholder.Persistence/Seeding/DefaultMenuSeeder.cs` and add a new `Menu` with `RequiredPermission` set to the permission string (e.g. `reports.read`).
  - The seeder is idempotent and will upsert the menu on startup.

- Option B: Admin API (runtime)
  - Use the new MenuController create endpoint:

```http
POST /api/menu
Authorization: Bearer <token with menus.create>
Content-Type: application/json

{
  "title": "Reports",
  "url": "/reports",
  "icon": "chart-bar",
  "requiredPermission": "reports.read",
  "parentMenuId": null
}
```

  - Users who have the `reports.read` permission will see the menu when calling `GET /api/menu`.

5) Assign permission to users/roles (if not using SuperAdmin)
- Use the Roles/Permissions endpoints to add the permission to a role, or add the role to the desired user.

6) Verify
- Authenticate as a user that has the new permission and call `GET /api/menu` — the new menu item should appear.

Notes
- The `MenuController` enforces `menus.create`/`menus.update`/`menus.delete` policies for management endpoints.
- If you add a permission constant, it will be registered automatically because startup code iterates `Permissions.All` to build authorization policies.

If you want, I can also add an example seeder entry for a specific menu item now.