using System.Data.Common;

namespace ProjectNamePlaceholder.Persistence.Dapper;

public interface IDapperDbSession : IAsyncDisposable, IDisposable
{
    DbConnection Connection { get; }
    DbTransaction? Transaction { get; }
    DbTransaction BeginTransaction();
    Task<DbTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task<int> CommitAsync(CancellationToken cancellationToken = default);
}
