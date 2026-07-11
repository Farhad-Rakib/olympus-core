using Dapper;
using ProjectNamePlaceholder.Application.Common.Interfaces;
using ProjectNamePlaceholder.Domain.Entities;
using static ProjectNamePlaceholder.Persistence.Dapper.DomainEntityHydrator;

namespace ProjectNamePlaceholder.Persistence.Dapper;

public sealed class DapperUserRepository : IUserRepository
{
    private readonly IDapperDbSession _session;

    public DapperUserRepository(IDapperDbSession session)
    {
        _session = session;
    }

    public async Task<User> AddAsync(User entity, CancellationToken cancellationToken = default)
    {
        const string insertUserSql = """
            INSERT INTO users ("Id", "FullName", "Email", "PasswordHash", "IsActive", "CreatedAtUtc", "UpdatedAtUtc")
            VALUES (@Id, @FullName, @Email, @PasswordHash, @IsActive, @CreatedAtUtc, @UpdatedAtUtc);
            """;

        const string insertUserRoleSql = """
            INSERT INTO user_roles ("UserId", "RoleId")
            VALUES (@UserId, @RoleId);
            """;

        var transaction = await _session.BeginTransactionAsync(cancellationToken);

        await _session.Connection.ExecuteAsync(new CommandDefinition(insertUserSql, new
        {
            entity.Id,
            entity.FullName,
            entity.Email,
            entity.PasswordHash,
            entity.IsActive,
            entity.CreatedAt,
            entity.UpdatedAt
        }, transaction, cancellationToken: cancellationToken));

        foreach (var userRole in entity.UserRoles)
        {
            await _session.Connection.ExecuteAsync(new CommandDefinition(insertUserRoleSql, new { UserId = entity.Id, userRole.RoleId }, transaction, cancellationToken: cancellationToken));
        }

        return entity;
    }

    public async Task<User?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT "Id", "FullName", "Email", "PasswordHash", "IsActive", "CreatedAtUtc", "UpdatedAtUtc"
            FROM users
            WHERE "Id" = @Id;
            """;

        var row = await _session.Connection.QuerySingleOrDefaultAsync<UserRow>(new CommandDefinition(sql, new { Id = id }, transaction: _session.Transaction, cancellationToken: cancellationToken));
        return row is null ? null : CreateUser(row);
    }

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string usersSql = """
            SELECT "Id", "FullName", "Email", "PasswordHash", "IsActive", "CreatedAtUtc", "UpdatedAtUtc"
            FROM users;
            """;

        const string rolesSql = """
            SELECT ur."UserId", r."Id", r."Name", r."Description"
            FROM user_roles ur
            INNER JOIN roles r ON r."Id" = ur."RoleId";
            """;

        var users = (await _session.Connection.QueryAsync<UserRow>(new CommandDefinition(usersSql, transaction: _session.Transaction, cancellationToken: cancellationToken)))
            .Select(CreateUser)
            .ToDictionary(u => u.Id);

        var roleRows = await _session.Connection.QueryAsync<(long UserId, long Id, string Name, string Description)>(new CommandDefinition(rolesSql, transaction: _session.Transaction, cancellationToken: cancellationToken));
        foreach (var roleRow in roleRows)
        {
            if (!users.TryGetValue(roleRow.UserId, out var user))
            {
                continue;
            }

            var role = CreateRole(new RoleRow(roleRow.Id, roleRow.Name, roleRow.Description));
            user.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id, User = user, Role = role });
        }

        return users.Values.ToList();
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT "Id", "FullName", "Email", "PasswordHash", "IsActive", "CreatedAtUtc", "UpdatedAtUtc"
            FROM users
            WHERE "Email" = @Email;
            """;

        var row = await _session.Connection.QuerySingleOrDefaultAsync<UserRow>(new CommandDefinition(sql, new { Email = email }, transaction: _session.Transaction, cancellationToken: cancellationToken));
        if (row is null)
        {
            return null;
        }

        var user = CreateUser(row);
        await PopulateRolesAsync(_session, user, cancellationToken);
        return user;
    }

    public async Task<User?> GetByIdWithRolesAsync(long id, CancellationToken cancellationToken = default)
    {
        var user = await GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            return null;
        }

        await PopulateRolesAsync(_session, user, cancellationToken);
        return user;
    }

    public void Update(User entity)
    {
        const string sql = """
            UPDATE users
            SET "FullName" = @FullName,
                "Email" = @Email,
                "PasswordHash" = @PasswordHash,
                "IsActive" = @IsActive,
                "UpdatedAtUtc" = @UpdatedAtUtc
            WHERE "Id" = @Id;
            """;

        var transaction = _session.BeginTransaction();
        _session.Connection.Execute(sql, new
        {
            entity.Id,
            entity.FullName,
            entity.Email,
            entity.PasswordHash,
            entity.IsActive,
            entity.UpdatedAt
        }, transaction);
    }

    public void Delete(User entity)
    {
        const string sql = """
            DELETE FROM users
            WHERE "Id" = @Id;
            """;

        var transaction = _session.BeginTransaction();
        _session.Connection.Execute(sql, new { entity.Id }, transaction);
    }

    private static async Task PopulateRolesAsync(IDapperDbSession session, User user, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT r."Id", r."Name", r."Description"
            FROM user_roles ur
            INNER JOIN roles r ON r."Id" = ur."RoleId"
            WHERE ur."UserId" = @UserId;
            """;

        var roles = await session.Connection.QueryAsync<RoleRow>(new CommandDefinition(sql, new { UserId = user.Id }, transaction: session.Transaction, cancellationToken: cancellationToken));
        foreach (var roleRow in roles)
        {
            var role = CreateRole(roleRow);
            user.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = role.Id,
                User = user,
                Role = role
            });
        }
    }
}
