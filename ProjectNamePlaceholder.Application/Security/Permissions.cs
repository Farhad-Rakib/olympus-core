namespace ProjectNamePlaceholder.Application.Security;

public static class Permissions
{
    public const string UsersRead = "users.read";
    public const string UsersCreate = "users.create";
    public const string UsersUpdate = "users.update";
    public const string UsersDelete = "users.delete";
    public const string RolesRead = "roles.read";
    public const string RolesCreate = "roles.create";
    public const string RolesUpdate = "roles.update";
    public const string RolesDelete = "roles.delete";
    public const string PermissionsRead = "permissions.read";
    public const string PermissionsCreate = "permissions.create";
    public const string PermissionsUpdate = "permissions.update";
    public const string PermissionsDelete = "permissions.delete";
    public const string UserRolesRead = "user-roles.read";
    public const string UserRolesCreate = "user-roles.create";
    public const string UserRolesUpdate = "user-roles.update";
    public const string UserRolesDelete = "user-roles.delete";
    public const string RolePermissionsRead = "role-permissions.read";
    public const string RolePermissionsCreate = "role-permissions.create";
    public const string RolePermissionsUpdate = "role-permissions.update";
    public const string RolePermissionsDelete = "role-permissions.delete";
    public const string SystemEndpointsRead = "system.endpoints.read";
    public const string SystemCacheRead = "system.cache.read";
    public const string SystemCacheFlush = "system.cache.flush";
    public const string MenusRead = "menus.read";
    public const string MenusCreate = "menus.create";
    public const string MenusUpdate = "menus.update";
    public const string MenusDelete = "menus.delete";
    public const string ReportsRead = "reports.read";
    public const string SiteSettingsRead = "site-settings.read";
    public const string SiteSettingsCreate = "site-settings.create";
    public const string SiteSettingsUpdate = "site-settings.update";
    public const string SiteSettingsDelete = "site-settings.delete";

    public static readonly string[] All =
    [
        UsersRead,
        UsersCreate,
        UsersUpdate,
        UsersDelete,
        RolesRead,
        RolesCreate,
        RolesUpdate,
        RolesDelete,
        PermissionsRead,
        PermissionsCreate,
        PermissionsUpdate,
        PermissionsDelete,
        UserRolesRead,
        UserRolesCreate,
        UserRolesUpdate,
        UserRolesDelete,
        RolePermissionsRead,
        RolePermissionsCreate,
        RolePermissionsUpdate,
        RolePermissionsDelete,
        MenusRead,
        MenusCreate,
        MenusUpdate,
        MenusDelete,
        ReportsRead,
        SiteSettingsRead,
        SiteSettingsCreate,
        SiteSettingsUpdate,
        SiteSettingsDelete,
        SystemEndpointsRead,
        SystemCacheRead,
        SystemCacheFlush
    ];
}
