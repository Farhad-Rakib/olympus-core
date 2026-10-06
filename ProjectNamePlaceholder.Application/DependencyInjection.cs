using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using ProjectNamePlaceholder.Application.Auth;
using ProjectNamePlaceholder.Application.Menu;
using ProjectNamePlaceholder.Application.Permissions;
using ProjectNamePlaceholder.Application.Common.Interfaces;
using ProjectNamePlaceholder.Application.Roles;
using ProjectNamePlaceholder.Application.SiteSettings;
using ProjectNamePlaceholder.Application.Users;

namespace ProjectNamePlaceholder.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ProjectNamePlaceholder.Application.Auth.External.IExternalAuthService, ProjectNamePlaceholder.Application.Auth.External.ExternalAuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<ISiteSettingService, SiteSettingService>();
        services.AddScoped<IMenuService, MenuService>();
        services.AddScoped<ProjectNamePlaceholder.Application.Notifications.INotificationService, ProjectNamePlaceholder.Application.Notifications.NotificationService>();
        services.AddScoped<ProjectNamePlaceholder.Application.Dashboard.IDashboardService, ProjectNamePlaceholder.Application.Dashboard.DashboardService>();
        services.AddScoped<IAuditLogService, ProjectNamePlaceholder.Application.Audit.AuditLogService>();
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}
