using Dapper;
using ProjectNamePlaceholder.Application.Common.Interfaces;
using ProjectNamePlaceholder.Domain.Entities;

namespace ProjectNamePlaceholder.Persistence.Dapper;

public sealed class DapperSiteSettingRepository : ISiteSettingRepository
{
    private readonly IDapperDbSession _session;

    public DapperSiteSettingRepository(IDapperDbSession session)
    {
        _session = session;
    }

    public async Task<SiteSetting> AddAsync(SiteSetting entity, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO site_settings ("Id", "Key", "Value", "Description")
            VALUES (@Id, @Key, @Value, @Description);
            """;

        var transaction = await _session.BeginTransactionAsync(cancellationToken);
        await _session.Connection.ExecuteAsync(new CommandDefinition(sql, entity, transaction, cancellationToken: cancellationToken));

        return entity;
    }

    public async Task<SiteSetting?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT "Id", "Key", "Value", "Description"
            FROM site_settings
            WHERE "Id" = @Id;
            """;

        return await _session.Connection.QuerySingleOrDefaultAsync<SiteSetting>(
            new CommandDefinition(sql, new { Id = id }, transaction: _session.Transaction, cancellationToken: cancellationToken));
    }

    public async Task<SiteSetting?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT "Id", "Key", "Value", "Description"
            FROM site_settings
            WHERE "Key" = @Key;
            """;

        return await _session.Connection.QuerySingleOrDefaultAsync<SiteSetting>(
            new CommandDefinition(sql, new { Key = key }, transaction: _session.Transaction, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<SiteSetting>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT "Id", "Key", "Value", "Description"
            FROM site_settings
            ORDER BY "Key";
            """;

        var settings = await _session.Connection.QueryAsync<SiteSetting>(
            new CommandDefinition(sql, transaction: _session.Transaction, cancellationToken: cancellationToken));
        return settings.ToList();
    }

    public void Update(SiteSetting entity)
    {
        const string sql = """
            UPDATE site_settings
            SET "Key" = @Key,
                "Value" = @Value,
                "Description" = @Description
            WHERE "Id" = @Id;
            """;

        var transaction = _session.BeginTransaction();
        _session.Connection.Execute(sql, entity, transaction);
    }

    public void Delete(SiteSetting entity)
    {
        const string sql = """
            DELETE FROM site_settings
            WHERE "Id" = @Id;
            """;

        var transaction = _session.BeginTransaction();
        _session.Connection.Execute(sql, new { entity.Id }, transaction);
    }
}
