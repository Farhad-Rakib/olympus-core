using Dapper;
using ProjectNamePlaceholder.Application.Common.Interfaces;
using ProjectNamePlaceholder.Domain.Entities;
using static ProjectNamePlaceholder.Persistence.Dapper.DomainEntityHydrator;

namespace ProjectNamePlaceholder.Persistence.Dapper;

public sealed class DapperRefreshTokenRepository : IRefreshTokenRepository
{
    private readonly IDapperDbSession _session;

    public DapperRefreshTokenRepository(IDapperDbSession session)
    {
        _session = session;
    }

    public async Task<RefreshToken> AddAsync(RefreshToken entity, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO refresh_tokens ("Id", "UserId", "TokenHash", "ExpiresAtUtc", "CreatedAtUtc", "RevokedAtUtc", "ReplacedByTokenHash")
            VALUES (@Id, @UserId, @TokenHash, @ExpiresAtUtc, @CreatedAtUtc, @RevokedAtUtc, @ReplacedByTokenHash);
            """;

        var transaction = await _session.BeginTransactionAsync(cancellationToken);
        await _session.Connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            entity.Id,
            entity.UserId,
            entity.TokenHash,
            entity.ExpiresAtUtc,
            entity.CreatedAt,
            entity.RevokedAtUtc,
            entity.ReplacedByTokenHash
        }, transaction, cancellationToken: cancellationToken));

        return entity;
    }

    public async Task<RefreshToken?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT "Id", "UserId", "TokenHash", "ExpiresAtUtc", "CreatedAtUtc", "RevokedAtUtc", "ReplacedByTokenHash"
            FROM refresh_tokens
            WHERE "Id" = @Id;
            """;

        var row = await _session.Connection.QuerySingleOrDefaultAsync<RefreshTokenRow>(new CommandDefinition(sql, new { Id = id }, transaction: _session.Transaction, cancellationToken: cancellationToken));
        return row is null ? null : CreateRefreshToken(row);
    }

    public async Task<IReadOnlyList<RefreshToken>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT "Id", "UserId", "TokenHash", "ExpiresAtUtc", "CreatedAtUtc", "RevokedAtUtc", "ReplacedByTokenHash"
            FROM refresh_tokens;
            """;

        var rows = await _session.Connection.QueryAsync<RefreshTokenRow>(new CommandDefinition(sql, transaction: _session.Transaction, cancellationToken: cancellationToken));
        return rows.Select(CreateRefreshToken).ToList();
    }

    public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT "Id", "UserId", "TokenHash", "ExpiresAtUtc", "CreatedAtUtc", "RevokedAtUtc", "ReplacedByTokenHash"
            FROM refresh_tokens
            WHERE "TokenHash" = @TokenHash;
            """;

        var row = await _session.Connection.QuerySingleOrDefaultAsync<RefreshTokenRow>(new CommandDefinition(sql, new { TokenHash = tokenHash }, transaction: _session.Transaction, cancellationToken: cancellationToken));
        return row is null ? null : CreateRefreshToken(row);
    }

    public void Update(RefreshToken entity)
    {
        const string sql = """
            UPDATE refresh_tokens
            SET "RevokedAtUtc" = @RevokedAtUtc,
                "ReplacedByTokenHash" = @ReplacedByTokenHash,
                "ExpiresAtUtc" = @ExpiresAtUtc
            WHERE "Id" = @Id;
            """;

        var transaction = _session.BeginTransaction();
        _session.Connection.Execute(sql, new
        {
            entity.Id,
            entity.RevokedAtUtc,
            entity.ReplacedByTokenHash,
            entity.ExpiresAtUtc
        }, transaction);
    }

    public void Delete(RefreshToken entity)
    {
        const string sql = """
            DELETE FROM refresh_tokens
            WHERE "Id" = @Id;
            """;

        var transaction = _session.BeginTransaction();
        _session.Connection.Execute(sql, new { entity.Id }, transaction);
    }
}
