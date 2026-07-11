using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProjectNamePlaceholder.Persistence.Context;

namespace ProjectNamePlaceholder.Persistence.Seeding;


public sealed class EfCoreDatabaseBootstrapper : IDatabaseBootstrapper
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IRbacSeeder _rbacSeeder;
    private readonly Microsoft.Extensions.Logging.ILogger<EfCoreDatabaseBootstrapper> _logger;

    public EfCoreDatabaseBootstrapper(
        ApplicationDbContext dbContext,
        IRbacSeeder rbacSeeder,
        Microsoft.Extensions.Logging.ILogger<EfCoreDatabaseBootstrapper> logger)
    {
        _dbContext = dbContext;
        _rbacSeeder = rbacSeeder;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger?.Log(LogLevel.Debug, "Attempting database connection and migration...");
        // Always use MigrateAsync - it handles both initial creation and applies pending migrations
        // while properly tracking them in __EFMigrationsHistory table
        await _dbContext.Database.MigrateAsync(cancellationToken);
        _logger?.Log(LogLevel.Debug, "Database migration applied successfully.");

        _logger?.Log(LogLevel.Debug, "Seeding RBAC data...");
        await _rbacSeeder.SeedAsync(cancellationToken);
        _logger?.Log(LogLevel.Debug, "RBAC seeding complete.");

        _logger?.Log(LogLevel.Debug, "Seeding default user...");
        var passwordHasher = new ProjectNamePlaceholder.Infrastructure.Security.Pbkdf2PasswordHasher();
        await DefaultUserSeeder.SeedAsync(_dbContext, passwordHasher);
        _logger?.Log(LogLevel.Debug, "Default user seeding complete.");

        _logger?.Log(LogLevel.Debug, "Seeding site settings...");
        await DefaultSiteSettingsSeeder.SeedAsync(_dbContext);
        _logger?.Log(LogLevel.Debug, "Site settings seeding complete.");

        _logger?.Log(LogLevel.Debug, "Seeding menu data...");
        await DefaultMenuSeeder.SeedAsync(_dbContext);
        _logger?.Log(LogLevel.Debug, "Menu seeding complete.");
    }
}
