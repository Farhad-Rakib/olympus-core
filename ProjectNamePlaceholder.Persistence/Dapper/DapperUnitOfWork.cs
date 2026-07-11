using ProjectNamePlaceholder.Application.Common.Interfaces;

namespace ProjectNamePlaceholder.Persistence.Dapper;

public sealed class DapperUnitOfWork : IUnitOfWork
{
    private readonly IDapperDbSession _session;

    public DapperUnitOfWork(IDapperDbSession session)
    {
        _session = session;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _session.CommitAsync(cancellationToken);
    }
}
