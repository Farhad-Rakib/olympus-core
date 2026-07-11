using System.Data.Common;

namespace ProjectNamePlaceholder.Persistence.Dapper;

public interface ISqlConnectionFactory
{
    string Provider { get; }
    DbConnection CreateOpenConnection();
}
