using Microsoft.EntityFrameworkCore;
using ProjectNamePlaceholder.Application.Common.Interfaces;
using ProjectNamePlaceholder.Domain.Entities;
using ProjectNamePlaceholder.Persistence.Context;

namespace ProjectNamePlaceholder.Persistence.Repositories;

public sealed class SiteSettingRepository : ISiteSettingRepository
{
    private readonly ApplicationDbContext _dbContext;

    public SiteSettingRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SiteSetting> AddAsync(SiteSetting entity, CancellationToken cancellationToken = default)
    {
        await _dbContext.SiteSettings.AddAsync(entity, cancellationToken);
        return entity;
    }

    public async Task<SiteSetting?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.SiteSettings.FirstOrDefaultAsync(setting => setting.Id == id, cancellationToken);
    }

    public async Task<SiteSetting?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        return await _dbContext.SiteSettings.FirstOrDefaultAsync(setting => setting.Key == key, cancellationToken);
    }

    public async Task<IReadOnlyList<SiteSetting>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SiteSettings
            .AsNoTracking()
            .OrderBy(setting => setting.Key)
            .ToListAsync(cancellationToken);
    }

    public void Update(SiteSetting entity)
    {
        _dbContext.SiteSettings.Update(entity);
    }

    public void Delete(SiteSetting entity)
    {
        _dbContext.SiteSettings.Remove(entity);
    }
}
