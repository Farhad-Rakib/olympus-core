namespace ProjectNamePlaceholder.Persistence.Seeding;

public sealed class DapperDatabaseBootstrapper : IDatabaseBootstrapper
{
    private readonly IRbacSeeder _rbacSeeder;

    public DapperDatabaseBootstrapper(IRbacSeeder rbacSeeder)
    {
        _rbacSeeder = rbacSeeder;
    }

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        return _rbacSeeder.SeedAsync(cancellationToken);
    }
}
