using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ProjectNamePlaceholder.Persistence.Context;

public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var provider = "postgres".ToLowerInvariant();

    //#if (database == "postgres")
        const string postgresConnection = "Host=localhost;Port=5432;Database=ProjectNamePlaceholderDb;Username=postgres;Password=postgres";
    //#endif
    //#if (database == "sqlserver")
        const string sqlServerConnection = "Server=localhost,1433;Database=ProjectNamePlaceholderDb;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True";
    //#endif

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
    //#if (database == "sqlserver")
        if (provider == "sqlserver")
        {
            optionsBuilder.UseSqlServer(sqlServerConnection);
        }
    //#elseif (database == "postgres")
        if (provider == "postgres")
        {
            optionsBuilder.UseNpgsql(postgresConnection);
        }
    //#endif
        else
        {
            throw new InvalidOperationException($"Unsupported database provider '{provider}'. Supported values: postgres, sqlserver.");
        }

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
