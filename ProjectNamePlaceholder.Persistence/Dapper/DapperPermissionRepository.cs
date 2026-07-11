using Dapper;
using ProjectNamePlaceholder.Application.Common.Interfaces;
using ProjectNamePlaceholder.Domain.Entities;
using static ProjectNamePlaceholder.Persistence.Dapper.DomainEntityHydrator;

namespace ProjectNamePlaceholder.Persistence.Dapper;

public sealed class DapperPermissionRepository : IPermissionRepository
{
    private readonly IDapperDbSession _session;

    public DapperPermissionRepository(IDapperDbSession session)
    {
        _session = session;
    }

    public async Task<Permission> AddAsync(Permission entity, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO permissions ("Id", "Name", "Description")
            VALUES (@Id, @Name, @Description);
            """;

        var transaction = await _session.BeginTransactionAsync(cancellationToken);
        await _session.Connection.ExecuteAsync(new CommandDefinition(sql, new { entity.Id, entity.Name, entity.Description }, transaction, cancellationToken: cancellationToken));
        return entity;
    }

    public async Task<Permission?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT "Id", "Name", "Description"
            FROM permissions
            WHERE "Id" = @Id;
            """;

        var row = await _session.Connection.QuerySingleOrDefaultAsync<PermissionRow>(new CommandDefinition(sql, new { Id = id }, transaction: _session.Transaction, cancellationToken: cancellationToken));
        return row is null ? null : CreatePermission(row);
    }

    public async Task<IReadOnlyList<Permission>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT "Id", "Name", "Description"
            FROM permissions;
            """;

        var rows = await _session.Connection.QueryAsync<PermissionRow>(new CommandDefinition(sql, transaction: _session.Transaction, cancellationToken: cancellationToken));
        return rows.Select(CreatePermission).ToList();
    }

    public async Task<Permission?> GetByNameAsync(string permissionName, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT "Id", "Name", "Description"
            FROM permissions
            WHERE "Name" = @Name
            LIMIT 1;
            """;

        var row = await _session.Connection.QuerySingleOrDefaultAsync<PermissionRow>(new CommandDefinition(sql, new { Name = permissionName }, transaction: _session.Transaction, cancellationToken: cancellationToken));
        return row is null ? null : CreatePermission(row);
    }

    public async Task<IReadOnlyList<string>> GetPermissionNamesForUserAsync(long userId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT DISTINCT p."Name"
            FROM user_roles ur
            INNER JOIN role_permissions rp ON rp."RoleId" = ur."RoleId"
            INNER JOIN permissions p ON p."Id" = rp."PermissionId"
            WHERE ur."UserId" = @UserId;
            """;

        var names = await _session.Connection.QueryAsync<string>(new CommandDefinition(sql, new { UserId = userId }, transaction: _session.Transaction, cancellationToken: cancellationToken));
        return names.ToList();
    }

    public void Update(Permission entity)
    {
        const string sql = """
            UPDATE permissions
            SET "Name" = @Name,
                "Description" = @Description
            WHERE "Id" = @Id;
            """;

        var transaction = _session.BeginTransaction();
        _session.Connection.Execute(sql, new { entity.Id, entity.Name, entity.Description }, transaction);
    }

    public void Delete(Permission entity)
    {
        const string sql = """
            DELETE FROM permissions
            WHERE "Id" = @Id;
            """;

        var transaction = _session.BeginTransaction();
        _session.Connection.Execute(sql, new { entity.Id }, transaction);
    }
}
