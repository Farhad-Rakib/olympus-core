using System.Data.Common;

namespace ProjectNamePlaceholder.Persistence.Dapper;

public sealed class DapperDbSession : IDapperDbSession
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private DbConnection? _connection;
    private DbTransaction? _transaction;

    public DapperDbSession(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public DbConnection Connection => _connection ??= _connectionFactory.CreateOpenConnection();

    public DbTransaction? Transaction => _transaction;

    public DbTransaction BeginTransaction()
    {
        _transaction ??= Connection.BeginTransaction();
        return _transaction;
    }

    public async Task<DbTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction ??= await Connection.BeginTransactionAsync(cancellationToken);
        return _transaction;
    }

    public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
        {
            return 0;
        }

        await _transaction.CommitAsync(cancellationToken);
        await _transaction.DisposeAsync();
        _transaction = null;
        return 1;
    }

    public void Dispose()
    {
        if (_transaction is not null)
        {
            try
            {
                _transaction.Rollback();
            }
            catch (InvalidOperationException)
            {
            }
            catch (DbException)
            {
            }
        }

        _transaction?.Dispose();
        _connection?.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaction is not null)
        {
            try
            {
                await _transaction.RollbackAsync();
            }
            catch (InvalidOperationException)
            {
            }
            catch (DbException)
            {
            }

            await _transaction.DisposeAsync();
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }
    }
}
