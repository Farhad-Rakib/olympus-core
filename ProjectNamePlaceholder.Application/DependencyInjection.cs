using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using ProjectNamePlaceholder.Application.Auth;
using ProjectNamePlaceholder.Application.Menu;
using ProjectNamePlaceholder.Application.Permissions;
using ProjectNamePlaceholder.Application.Roles;
using ProjectNamePlaceholder.Application.SiteSettings;
using ProjectNamePlaceholder.Application.Users;

namespace ProjectNamePlaceholder.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<ISiteSettingService, SiteSettingService>();
        services.AddScoped<IMenuService, MenuService>();
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}
