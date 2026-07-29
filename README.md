
# OlympusCore .NET Template

OlympusCore is a reusable `dotnet new` template that generates a production-ready Clean Architecture Web API for .NET projects.

## 1. Install the Template

**Recommended (clone and install):**

```sh
git clone https://github.com/Farhad-Rakib/olympus-core.git
cd olympus-core
dotnet new install .
```

**Direct install from GitHub URL (if supported):**

```sh
dotnet new install https://github.com/Farhad-Rakib/olympus-core.git
```

**Update to latest version:**

```sh
dotnet new uninstall .
git pull
dotnet new install .
```

**Verify installation:**

```sh
dotnet new list olympuscore
```

## 2. Create a New Project from the Template

```sh
dotnet new olympuscore -n MyCompany.Orders
```

**Options:**

- `--database postgres|sqlserver` (choose database provider)
- `--orm efcore|dapper` (choose ORM)
- `--enableSeq true|false` (enable/disable Seq logging)

**Example:**

```sh
dotnet new olympuscore -n MyCompany.Orders --database postgres --orm efcore --enableSeq true
```

## 3. Configure the Generated Application

Edit the generated API settings file to configure:

- DataAccess:Orm
- Database:Provider
- ConnectionStrings:PostgresConnection
- ConnectionStrings:SqlServerConnection
- Jwt:SecretKey (min 32 chars recommended)
- Jwt:Issuer
- Jwt:Audience
- Smtp settings
- Serilog:EnableSeq and Serilog:SeqServerUrl (optional)

## 4. Build and Run the App

```sh
dotnet restore
dotnet build
dotnet run --project src/MyCompany.Orders.Api
```

Swagger UI is available in development mode after app startup.

---

## 5. How to Add or Customize Menus

The template provides a flexible menu system for your application. To add or customize menus:

1. **Edit Menu Definitions:**
    - For static menus, update `MenuProvider.cs` in `src/ProjectName.Application/Menu/`.
    - For database-driven menus, update the `Menu` entity and seed data in the database.
2. **Menu Structure:**
    - Each menu item can have a title, URL, icon, required permission, and children.
    - Example (static):

      ```csharp
      new MenuItemDto { Title = "Users", Url = "/users", Icon = "users", RequiredPermission = Permissions.UsersRead }
      ```
3. **API Endpoint:**
    - The API exposes `GET /api/menu` to fetch the menu for the current user, filtered by permissions.
4. **Frontend Integration:**
    - Call the menu endpoint after login to build the navigation UI dynamically based on user permissions.

---

## What this template generates

- Clean Architecture layers
- Selectable persistence with EF Core or Dapper
- Selectable database provider: PostgreSQL via Npgsql or SQL Server
- User profile management (view/update profile, profile image, roles)
- Forgot password (request reset via email, reset with token)
- Change password (authenticated user)
- Site settings management (CRUD for global app settings)

---

## Notification Usage

The generated API supports real-time notifications using SignalR and also provides REST endpoints for notification management.

- SignalR hub endpoint: `/hubs/notifications`
        - Connect from your frontend using the user's JWT token for authentication.
        - Listen for the `ReceiveNotification` event to get new notifications instantly.

- REST endpoints:
        - `GET /api/v1/notifications` — List notifications for the current user
        - `POST /api/v1/notifications` — Create a notification (admin/system)
        - `POST /api/v1/notifications/{id}/read` — Mark as read
        - `DELETE /api/v1/notifications/{id}` — Delete a notification

**Example SignalR client (JavaScript):**

```js
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/notifications", { accessTokenFactory: () => jwtToken })
    .build();

connection.on("ReceiveNotification", notification => {
    // Handle notification (show toast, update UI, etc.)
});

await connection.start();
```

Use these features to deliver in-app, real-time, and persistent notifications to your users.


## Default User and Site Settings

The generated project seeds a default admin user and a set of industry-standard site settings for easy testing and configuration:

- **Default user:**
    - Email: `admin@localhost`
    - Password: `admin@123`
    - Name: `Admin`
    - (Assigned admin role if available)

- **Default site settings include:**
    - Site title, logo URL, tagline
    - Sidebar position and collapse state
    - Navbar freeze, accordion enabled
    - Color scheme (light/dark/auto)
    - SMTP host, port, username, password, from email/name

## Managing Site Settings

You can manage site settings via the provided API endpoints (see below). When the frontend loads for the first time, it should fetch all site settings from the API and store them in a local JSON object. On subsequent loads, the frontend can use the cached JSON for fast access. If any site setting is changed via the API, the frontend should update its local JSON cache accordingly.

This approach ensures the frontend always reflects the latest configuration while minimizing API calls.

The generated API includes endpoints for managing global site settings (key-value pairs):

- `GET /api/v1/sitesettings` — List all settings
- `GET /api/v1/sitesettings/{key}` — Get a setting by key
- `POST /api/v1/sitesettings` — Create or update a setting (body: SiteSettingDto)
- `DELETE /api/v1/sitesettings/{id}` — Delete a setting by ID

**Example SiteSettingDto:**

        {
            "id": "00000000-0000-0000-0000-000000000000", // Use empty for new
            "key": "SiteName",
            "value": "My App",
            "description": "The public name of the site."
        }

You can use these endpoints to store and retrieve any global configuration (branding, contact info, feature flags, etc.) at runtime.


## Solution Structure

    src/
      ProjectName.Api
      ProjectName.Application
      ProjectName.Domain
      ProjectName.Infrastructure
      ProjectName.Persistence

## Prerequisites

- .NET SDK 8.0+
- PostgreSQL or SQL Server (local or remote)
- Optional: Seq for log aggregation

Check SDK:

    dotnet --version




## Authentication workflow in generated API

- Login endpoint returns access token and refresh token
- Refresh endpoint rotates refresh token and issues a new token pair
- Revoke endpoint invalidates a refresh token
- Default endpoint format uses API versioning: /api/v1/{controller}

## Endpoint inventory endpoint

The generated API includes a secured endpoint that lists registered API routes.

- Route: /api/v1/system/endpoints
- Permission required: system.endpoints.read

## Database and seeding behavior

On startup, the generated API:

- Ensures database is created
- Seeds default RBAC roles and permissions when missing

Manual seeding flow for a fresh environment:

1. Configure connection string and provider in appsettings.
2. Run the API once: `dotnet run --project src/MyCompany.Orders.Api`.
3. Confirm the seed data exists (roles, permissions, and default RBAC mappings).

Re-seeding guidance:

1. Clear the target database schema/data.
2. Start the API again to trigger seeding on startup.

EF Core migrations and seed commands:

    dotnet ef migrations add InitialCreate --project src/MyCompany.Orders.Persistence --startup-project src/MyCompany.Orders.Api
    dotnet ef database update --project src/MyCompany.Orders.Persistence --startup-project src/MyCompany.Orders.Api
    dotnet run --project src/MyCompany.Orders.Api

Dapper seed command flow:

    dotnet run --project src/MyCompany.Orders.Api

Notes:

- For `--orm efcore`, run migrations first, then run the API to execute RBAC seeding.
- For `--orm dapper`, schema and RBAC seed are applied during API startup.


## Update or Uninstall Template

To update after local changes:

```sh
dotnet new uninstall .
dotnet new install .
```

To uninstall:

```sh
dotnet new uninstall .
```

## Troubleshooting

If template does not appear in list:

1. Run `dotnet new uninstall .`
2. Run `dotnet new install .`
3. Run `dotnet new list olympuscore`

If generated app fails JWT auth:

1. Verify Jwt:SecretKey is configured
2. Verify Issuer and Audience match token validation settings
3. Ensure system clock is accurate

If PostgreSQL connection fails:

1. Validate host, port, database, username, and password
2. Confirm PostgreSQL service is running
3. Check firewall and network rules


