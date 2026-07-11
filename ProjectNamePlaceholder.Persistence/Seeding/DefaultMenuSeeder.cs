using Microsoft.EntityFrameworkCore;
using ProjectNamePlaceholder.Application.Security;
using ProjectNamePlaceholder.Domain.Entities;
using ProjectNamePlaceholder.Persistence.Context;

namespace ProjectNamePlaceholder.Persistence.Seeding
{
    public static class DefaultMenuSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext dbContext)
        {
            var dashboard = await UpsertMenuAsync(dbContext, "Dashboard", "/dashboard", "dashboard", null, null);
            var configuration = await UpsertMenuAsync(dbContext, "Configuration", null, "settings", null, null);

            await UpsertMenuAsync(dbContext, "Users", "/users", "users", Permissions.UsersRead, configuration);
            await UpsertMenuAsync(dbContext, "Roles", "/roles", "roles", Permissions.RolesRead, configuration);
            await UpsertMenuAsync(dbContext, "Permissions", "/permissions", "permissions", Permissions.PermissionsRead, configuration);
            await UpsertMenuAsync(dbContext, "User Roles", "/users/roles", "user-roles", Permissions.UserRolesRead, configuration);
            await UpsertMenuAsync(dbContext, "Role Permissions", "/roles/permissions", "role-permissions", Permissions.RolePermissionsRead, configuration);
            // Example additional menu: Reports (requires reports.read permission)
            await UpsertMenuAsync(dbContext, "Reports", "/reports", "chart-bar", Permissions.ReportsRead, configuration);

            await dbContext.SaveChangesAsync();
        }

        private static async Task<Menu> UpsertMenuAsync(
            ApplicationDbContext dbContext,
            string title,
            string? url,
            string? icon,
            string? requiredPermission,
            Menu? parentMenu)
        {
            var menu = await dbContext.Menus.FirstOrDefaultAsync(existing => existing.Title == title);
            if (menu is null)
            {
                menu = new Menu { Title = title };
                dbContext.Menus.Add(menu);
            }

            menu.Url = url;
            menu.Icon = icon;
            menu.RequiredPermission = requiredPermission;
            menu.ParentMenu = parentMenu;
            menu.ParentMenuId = parentMenu?.Id;

            return menu;
        }
    }
}
