using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectNamePlaceholder.Application.Common.Interfaces;
//#if (orm == "efcore")
using Microsoft.EntityFrameworkCore;
using ProjectNamePlaceholder.Persistence.Context;
using ProjectNamePlaceholder.Persistence.Repositories;
//#endif
using ProjectNamePlaceholder.Persistence.Seeding;
using ProjectNamePlaceholder.Application.Menu;

namespace ProjectNamePlaceholder.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            var provider = GetProvider(configuration);
            //#if (database == "sqlserver")
            if (provider == "sqlserver")
            {
                options.UseSqlServer(GetConnectionString(configuration, "SqlServerConnection"));
            }
            //#elseif (database == "postgres")
            if (provider == "postgres")
            {
                options.UseNpgsql(GetConnectionString(configuration, "PostgresConnection"));
            }
            //#endif
            else
            {
                throw new InvalidOperationException($"Unsupported database provider '{provider}'. Supported values: postgres, sqlserver.");
            }
        });

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
        services.AddScoped<ISiteSettingRepository, SiteSettingRepository>();
        services.AddScoped(typeof(IRepository<>), typeof(BaseRepository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IMenuRepository, MenuRepository>();
        services.AddScoped<IRbacSeeder, RbacSeeder>();
        services.AddScoped<IDatabaseBootstrapper, EfCoreDatabaseBootstrapper>();
        services.AddScoped<IMenuRepository, MenuRepository>();

        return services;
    }

    private static string GetProvider(IConfiguration configuration)
    {
        var provider = (configuration["Database:Provider"] ?? "postgres").ToLowerInvariant();
        if (provider is not ("postgres" or "sqlserver"))
        {
            throw new InvalidOperationException($"Unsupported database provider '{provider}'. Supported values: postgres, sqlserver.");
        }

        return provider;
    }

    private static string GetConnectionString(IConfiguration configuration, string key)
    {
        return configuration.GetConnectionString(key)
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException($"Missing connection string: {key}.");
    }
}
