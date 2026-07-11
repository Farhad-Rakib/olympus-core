using ProjectNamePlaceholder.Domain.Entities;

namespace ProjectNamePlaceholder.Application.Common.Interfaces;

public interface ISiteSettingRepository
{
    Task<SiteSetting> AddAsync(SiteSetting entity, CancellationToken cancellationToken = default);
    Task<SiteSetting?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<SiteSetting?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SiteSetting>> GetAllAsync(CancellationToken cancellationToken = default);
    void Update(SiteSetting entity);
    void Delete(SiteSetting entity);
}
