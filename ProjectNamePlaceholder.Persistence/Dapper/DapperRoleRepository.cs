using Dapper;
using ProjectNamePlaceholder.Application.Common.Interfaces;
using ProjectNamePlaceholder.Domain.Entities;
using static ProjectNamePlaceholder.Persistence.Dapper.DomainEntityHydrator;

namespace ProjectNamePlaceholder.Persistence.Dapper;

public sealed class DapperRoleRepository : IRoleRepository
{
    private readonly IDapperDbSession _session;

    public DapperRoleRepository(IDapperDbSession session)
    {
        _session = session;
    }

    public async Task<Role> AddAsync(Role entity, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO roles ("Id", "Name", "Description")
            VALUES (@Id, @Name, @Description);
            """;

        var transaction = await _session.BeginTransactionAsync(cancellationToken);
        await _session.Connection.ExecuteAsync(new CommandDefinition(sql, new { entity.Id, entity.Name, entity.Description }, transaction, cancellationToken: cancellationToken));
        return entity;
    }

    public async Task<Role?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT "Id", "Name", "Description"
            FROM roles
            WHERE "Id" = @Id;
            """;

        var row = await _session.Connection.QuerySingleOrDefaultAsync<RoleRow>(new CommandDefinition(sql, new { Id = id }, transaction: _session.Transaction, cancellationToken: cancellationToken));
        return row is null ? null : CreateRole(row);
    }

    public async Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT "Id", "Name", "Description"
            FROM roles;
            """;

        var rows = await _session.Connection.QueryAsync<RoleRow>(new CommandDefinition(sql, transaction: _session.Transaction, cancellationToken: cancellationToken));
        return rows.Select(CreateRole).ToList();
    }

    public async Task<IReadOnlyList<Role>> GetByNamesAsync(IEnumerable<string> roleNames, CancellationToken cancellationToken = default)
    {
        var names = roleNames.Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (names.Length == 0)
        {
            return [];
        }

        const string sql = """
            SELECT "Id", "Name", "Description"
            FROM roles
            WHERE "Name" IN @Names;
            """;

        var rows = await _session.Connection.QueryAsync<RoleRow>(new CommandDefinition(sql, new { Names = names }, transaction: _session.Transaction, cancellationToken: cancellationToken));
        return rows.Select(CreateRole).ToList();
    }

    public async Task<Role?> GetByNameAsync(string roleName, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT "Id", "Name", "Description"
            FROM roles
            WHERE "Name" = @Name
            LIMIT 1;
            """;

        var row = await _session.Connection.QuerySingleOrDefaultAsync<RoleRow>(new CommandDefinition(sql, new { Name = roleName }, transaction: _session.Transaction, cancellationToken: cancellationToken));
        return row is null ? null : await HydrateRoleWithPermissionsAsync(row, cancellationToken);
    }

    public async Task<Role?> GetByIdWithPermissionsAsync(long id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT "Id", "Name", "Description"
            FROM roles
            WHERE "Id" = @Id
            LIMIT 1;
            """;

        var row = await _session.Connection.QuerySingleOrDefaultAsync<RoleRow>(new CommandDefinition(sql, new { Id = id }, transaction: _session.Transaction, cancellationToken: cancellationToken));
        return row is null ? null : await HydrateRoleWithPermissionsAsync(row, cancellationToken);
    }

    public void Update(Role entity)
    {
        const string sql = """
            UPDATE roles
            SET "Name" = @Name,
                "Description" = @Description
            WHERE "Id" = @Id;
            """;

        var transaction = _session.BeginTransaction();
        _session.Connection.Execute(sql, new { entity.Id, entity.Name, entity.Description }, transaction);
    }

    public void Delete(Role entity)
    {
        const string sql = """
            DELETE FROM roles
            WHERE "Id" = @Id;
            """;

        var transaction = _session.BeginTransaction();
        _session.Connection.Execute(sql, new { entity.Id }, transaction);
    }

    private async Task<Role> HydrateRoleWithPermissionsAsync(RoleRow row, CancellationToken cancellationToken)
    {
        var role = CreateRole(row);

        const string sql = """
            SELECT p."Id", p."Name", p."Description"
            FROM role_permissions rp
            INNER JOIN permissions p ON p."Id" = rp."PermissionId"
            WHERE rp."RoleId" = @RoleId;
            """;

        var permissionRows = await _session.Connection.QueryAsync<PermissionRow>(new CommandDefinition(sql, new { RoleId = role.Id }, transaction: _session.Transaction, cancellationToken: cancellationToken));
        foreach (var permission in permissionRows.Select(CreatePermission))
        {
            role.RolePermissions.Add(new RolePermission
            {
                RoleId = role.Id,
                PermissionId = permission.Id,
                Role = role,
                Permission = permission
            });
        }

        return role;
    }
}
