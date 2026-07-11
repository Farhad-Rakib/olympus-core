using System.Data.Common;
//#if (database == "sqlserver")
using Microsoft.Data.SqlClient;
//#endif
//#if (database == "postgres")
using Npgsql;
//#endif

namespace ProjectNamePlaceholder.Persistence.Dapper;

public sealed class SqlConnectionFactory : ISqlConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(string provider, string connectionString)
    {
        Provider = provider;
        _connectionString = connectionString;
    }

    public string Provider { get; }

    public DbConnection CreateOpenConnection()
    {
        DbConnection connection = Provider switch
        {
//#if (database == "sqlserver")
            "sqlserver" => new SqlConnection(_connectionString),
//#endif
//#if (database == "postgres")
            "postgres" => new NpgsqlConnection(_connectionString),
//#endif
            _ => throw new InvalidOperationException($"Unsupported database provider '{Provider}'.")
        };

        connection.Open();
        return connection;
    }
}
