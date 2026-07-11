namespace ProjectNamePlaceholder.Persistence.Seeding;

public interface IDatabaseBootstrapper
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
