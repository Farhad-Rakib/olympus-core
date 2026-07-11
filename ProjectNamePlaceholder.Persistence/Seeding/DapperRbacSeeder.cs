using Dapper;
using Microsoft.Extensions.Logging;
using ProjectNamePlaceholder.Application.Security;
using ProjectNamePlaceholder.Domain.Enums;
using ProjectNamePlaceholder.Persistence.Dapper;

namespace ProjectNamePlaceholder.Persistence.Seeding;

public sealed class DapperRbacSeeder : IRbacSeeder
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly ILogger<DapperRbacSeeder> _logger;

    public DapperRbacSeeder(ISqlConnectionFactory connectionFactory, ILogger<DapperRbacSeeder> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory.CreateOpenConnection();
        await EnsureSchemaAsync(connection, cancellationToken);

        // Seed site settings only if not exists
        var existingSiteSettings = await connection.ExecuteScalarAsync<int>(new CommandDefinition("SELECT COUNT(1) FROM site_settings;", cancellationToken: cancellationToken));
        if (existingSiteSettings == 0)
        {
            _logger.LogDebug("Seeding site settings...");
            await SeedSiteSettingsAsync(connection, cancellationToken);
            _logger.LogDebug("Site settings seeding complete.");
        }
        else
        {
            _logger.LogDebug("Site settings already exist. Skipping seeding.");
        }

        var existingRoles = await connection.ExecuteScalarAsync<int>(new CommandDefinition("SELECT COUNT(1) FROM roles;", cancellationToken: cancellationToken));
        var existingPermissions = await connection.ExecuteScalarAsync<int>(new CommandDefinition("SELECT COUNT(1) FROM permissions;", cancellationToken: cancellationToken));

        if (existingRoles > 0 && existingPermissions > 0)
        {
            _logger.LogDebug("RBAC data already exists. Skipping seeding.");
            return;
        }

        _logger.LogDebug("Seeding RBAC data...");
        var roles = new[]
        {
            new { Id = Guid.NewGuid(), Name = SystemRoles.SuperAdmin, Description = "Full system access." },
            new { Id = Guid.NewGuid(), Name = SystemRoles.Admin, Description = "Administrative management access." },
            new { Id = Guid.NewGuid(), Name = SystemRoles.User, Description = "Basic application user access." }
        };

        const string insertRoleSql = """
            INSERT INTO roles ("Id", "Name", "Description")
            VALUES (@Id, @Name, @Description);
            """;

        foreach (var role in roles)
        {
            await connection.ExecuteAsync(new CommandDefinition(insertRoleSql, role, cancellationToken: cancellationToken));
        }

        var permissions = Permissions.All
            .Select(name => new
            {
                Id = Guid.NewGuid(),
                Name = name,
                Description = $"Permission for {name}."
            })
            .ToList();

        const string insertPermissionSql = """
            INSERT INTO permissions ("Id", "Name", "Description")
            VALUES (@Id, @Name, @Description);
            """;

        foreach (var permission in permissions)
        {
            await connection.ExecuteAsync(new CommandDefinition(insertPermissionSql, permission, cancellationToken: cancellationToken));
        }


        var adminId = roles.Single(r => r.Name == SystemRoles.Admin).Id;
        var userId = roles.Single(r => r.Name == SystemRoles.User).Id;

        var rolePermissions = new List<object>();

        // NOTE: SuperAdmin bypasses permission checks at runtime; do NOT assign explicit permissions here.

        rolePermissions.AddRange(permissions
            .Where(p => p.Name is not Permissions.UsersDelete and not Permissions.RolesDelete and not Permissions.PermissionsDelete and not Permissions.UserRolesDelete and not Permissions.RolePermissionsDelete)
            .Select(permission => new { RoleId = adminId, PermissionId = permission.Id }));

        var usersReadPermission = permissions.Single(p => p.Name == Permissions.UsersRead).Id;
        rolePermissions.Add(new { RoleId = userId, PermissionId = usersReadPermission });

        const string insertRolePermissionSql = """
            INSERT INTO role_permissions ("RoleId", "PermissionId")
            VALUES (@RoleId, @PermissionId);
            """;

        foreach (var rolePermission in rolePermissions)
        {
            await connection.ExecuteAsync(new CommandDefinition(insertRolePermissionSql, rolePermission, cancellationToken: cancellationToken));
        }

        _logger.LogDebug("RBAC seeding complete.");
    }

    private async Task EnsureSchemaAsync(System.Data.Common.DbConnection connection, CancellationToken cancellationToken)
    {
        if (_connectionFactory.Provider == "sqlserver")
        {
            const string sqlServerSchemaSql = """
                IF OBJECT_ID('users', 'U') IS NULL
                BEGIN
                    CREATE TABLE users (
                        [Id] uniqueidentifier NOT NULL PRIMARY KEY,
                        [FullName] nvarchar(200) NOT NULL,
                        [Email] nvarchar(320) NOT NULL,
                        [PasswordHash] nvarchar(max) NOT NULL,
                        [IsActive] bit NOT NULL,
                        [CreatedAtUtc] datetime2 NOT NULL,
                        [UpdatedAtUtc] datetime2 NULL
                    );
                    CREATE UNIQUE INDEX IX_users_Email ON users ([Email]);
                END

                IF OBJECT_ID('roles', 'U') IS NULL
                BEGIN
                    CREATE TABLE roles (
                        [Id] uniqueidentifier NOT NULL PRIMARY KEY,
                        [Name] nvarchar(100) NOT NULL,
                        [Description] nvarchar(500) NOT NULL
                    );
                    CREATE UNIQUE INDEX IX_roles_Name ON roles ([Name]);
                END

                IF OBJECT_ID('permissions', 'U') IS NULL
                BEGIN
                    CREATE TABLE permissions (
                        [Id] uniqueidentifier NOT NULL PRIMARY KEY,
                        [Name] nvarchar(150) NOT NULL,
                        [Description] nvarchar(500) NOT NULL
                    );
                    CREATE UNIQUE INDEX IX_permissions_Name ON permissions ([Name]);
                END

                IF OBJECT_ID('user_roles', 'U') IS NULL
                BEGIN
                    CREATE TABLE user_roles (
                        [UserId] uniqueidentifier NOT NULL,
                        [RoleId] uniqueidentifier NOT NULL,
                        CONSTRAINT PK_user_roles PRIMARY KEY ([UserId], [RoleId]),
                        CONSTRAINT FK_user_roles_users FOREIGN KEY ([UserId]) REFERENCES users([Id]) ON DELETE CASCADE,
                        CONSTRAINT FK_user_roles_roles FOREIGN KEY ([RoleId]) REFERENCES roles([Id]) ON DELETE CASCADE
                    );
                END

                IF OBJECT_ID('role_permissions', 'U') IS NULL
                BEGIN
                    CREATE TABLE role_permissions (
                        [RoleId] uniqueidentifier NOT NULL,
                        [PermissionId] uniqueidentifier NOT NULL,
                        CONSTRAINT PK_role_permissions PRIMARY KEY ([RoleId], [PermissionId]),
                        CONSTRAINT FK_role_permissions_roles FOREIGN KEY ([RoleId]) REFERENCES roles([Id]) ON DELETE CASCADE,
                        CONSTRAINT FK_role_permissions_permissions FOREIGN KEY ([PermissionId]) REFERENCES permissions([Id]) ON DELETE CASCADE
                    );
                END

                IF OBJECT_ID('refresh_tokens', 'U') IS NULL
                BEGIN
                    CREATE TABLE refresh_tokens (
                        [Id] uniqueidentifier NOT NULL PRIMARY KEY,
                        [UserId] uniqueidentifier NOT NULL,
                        [TokenHash] nvarchar(128) NOT NULL,
                        [ExpiresAtUtc] datetime2 NOT NULL,
                        [CreatedAtUtc] datetime2 NOT NULL,
                        [RevokedAtUtc] datetime2 NULL,
                        [ReplacedByTokenHash] nvarchar(128) NULL,
                        CONSTRAINT FK_refresh_tokens_users FOREIGN KEY ([UserId]) REFERENCES users([Id]) ON DELETE CASCADE
                    );
                    CREATE UNIQUE INDEX IX_refresh_tokens_TokenHash ON refresh_tokens ([TokenHash]);
                    CREATE INDEX IX_refresh_tokens_UserId_ExpiresAtUtc ON refresh_tokens ([UserId], [ExpiresAtUtc]);
                END

                IF OBJECT_ID('site_settings', 'U') IS NULL
                BEGIN
                    CREATE TABLE site_settings (
                        [Id] uniqueidentifier NOT NULL PRIMARY KEY,
                        [Key] nvarchar(150) NOT NULL,
                        [Value] nvarchar(max) NOT NULL,
                        [Description] nvarchar(500) NULL
                    );
                    CREATE UNIQUE INDEX IX_site_settings_Key ON site_settings ([Key]);
                END
                """;

            await connection.ExecuteAsync(new CommandDefinition(sqlServerSchemaSql, cancellationToken: cancellationToken));
            return;
        }

        const string postgresSchemaSql = """
            CREATE TABLE IF NOT EXISTS users (
                "Id" uuid PRIMARY KEY,
                "FullName" varchar(200) NOT NULL,
                "Email" varchar(320) NOT NULL,
                "PasswordHash" text NOT NULL,
                "IsActive" boolean NOT NULL,
                "CreatedAtUtc" timestamp with time zone NOT NULL,
                "UpdatedAtUtc" timestamp with time zone NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_users_Email" ON users ("Email");

            CREATE TABLE IF NOT EXISTS roles (
                "Id" uuid PRIMARY KEY,
                "Name" varchar(100) NOT NULL,
                "Description" varchar(500) NOT NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_roles_Name" ON roles ("Name");

            CREATE TABLE IF NOT EXISTS permissions (
                "Id" uuid PRIMARY KEY,
                "Name" varchar(150) NOT NULL,
                "Description" varchar(500) NOT NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_permissions_Name" ON permissions ("Name");

            CREATE TABLE IF NOT EXISTS user_roles (
                "UserId" uuid NOT NULL,
                "RoleId" uuid NOT NULL,
                PRIMARY KEY ("UserId", "RoleId"),
                CONSTRAINT "FK_user_roles_users" FOREIGN KEY ("UserId") REFERENCES users("Id") ON DELETE CASCADE,
                CONSTRAINT "FK_user_roles_roles" FOREIGN KEY ("RoleId") REFERENCES roles("Id") ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS role_permissions (
                "RoleId" uuid NOT NULL,
                "PermissionId" uuid NOT NULL,
                PRIMARY KEY ("RoleId", "PermissionId"),
                CONSTRAINT "FK_role_permissions_roles" FOREIGN KEY ("RoleId") REFERENCES roles("Id") ON DELETE CASCADE,
                CONSTRAINT "FK_role_permissions_permissions" FOREIGN KEY ("PermissionId") REFERENCES permissions("Id") ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS refresh_tokens (
                "Id" uuid PRIMARY KEY,
                "UserId" uuid NOT NULL,
                "TokenHash" varchar(128) NOT NULL,
                "ExpiresAtUtc" timestamp with time zone NOT NULL,
                "CreatedAtUtc" timestamp with time zone NOT NULL,
                "RevokedAtUtc" timestamp with time zone NULL,
                "ReplacedByTokenHash" varchar(128) NULL,
                CONSTRAINT "FK_refresh_tokens_users" FOREIGN KEY ("UserId") REFERENCES users("Id") ON DELETE CASCADE
            );
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_refresh_tokens_TokenHash" ON refresh_tokens ("TokenHash");
            CREATE INDEX IF NOT EXISTS "IX_refresh_tokens_UserId_ExpiresAtUtc" ON refresh_tokens ("UserId", "ExpiresAtUtc");

            CREATE TABLE IF NOT EXISTS site_settings (
                "Id" uuid PRIMARY KEY,
                "Key" varchar(150) NOT NULL,
                "Value" text NOT NULL,
                "Description" varchar(500) NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_site_settings_Key" ON site_settings ("Key");
            """;

        await connection.ExecuteAsync(new CommandDefinition(postgresSchemaSql, cancellationToken: cancellationToken));
    }

    private static async Task SeedSiteSettingsAsync(System.Data.Common.DbConnection connection, CancellationToken cancellationToken)
    {
        var settings = new[]
        {
            new { Id = Guid.NewGuid(), Key = "Site.Title", Value = "OlympusCore App", Description = "Site title" },
            new { Id = Guid.NewGuid(), Key = "Site.LogoUrl", Value = "/assets/logo.png", Description = "Logo URL" },
            new { Id = Guid.NewGuid(), Key = "Site.Tagline", Value = "Your productivity, elevated.", Description = "Site tagline" },
            new { Id = Guid.NewGuid(), Key = "UI.Sidebar.Position", Value = "left", Description = "Sidebar position (left/right)" },
            new { Id = Guid.NewGuid(), Key = "UI.Sidebar.Collapsed", Value = "false", Description = "Sidebar collapsed by default" },
            new { Id = Guid.NewGuid(), Key = "UI.Navbar.Freeze", Value = "true", Description = "Navbar freeze enabled" },
            new { Id = Guid.NewGuid(), Key = "UI.Accordion.Enabled", Value = "true", Description = "Accordion enabled in UI" },
            new { Id = Guid.NewGuid(), Key = "UI.ColorScheme", Value = "light", Description = "App color scheme (light/dark/auto)" },
            new { Id = Guid.NewGuid(), Key = "Smtp.Host", Value = "smtp.example.com", Description = "SMTP server host" },
            new { Id = Guid.NewGuid(), Key = "Smtp.Port", Value = "587", Description = "SMTP server port" },
            new { Id = Guid.NewGuid(), Key = "Smtp.Username", Value = "user@example.com", Description = "SMTP username" },
            new { Id = Guid.NewGuid(), Key = "Smtp.Password", Value = "password", Description = "SMTP password" },
            new { Id = Guid.NewGuid(), Key = "Smtp.FromEmail", Value = "noreply@example.com", Description = "SMTP from email address" },
            new { Id = Guid.NewGuid(), Key = "Smtp.FromName", Value = "OlympusCore", Description = "SMTP from name" }
        };

        const string sql = """
            INSERT INTO site_settings ("Id", "Key", "Value", "Description")
            SELECT @Id, @Key, @Value, @Description
            WHERE NOT EXISTS (
                SELECT 1 FROM site_settings WHERE "Key" = @Key
            );
            """;

        foreach (var setting in settings)
        {
            await connection.ExecuteAsync(new CommandDefinition(sql, setting, cancellationToken: cancellationToken));
        }
    }
}
