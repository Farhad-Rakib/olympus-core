using ProjectNamePlaceholder.Application.SiteSettings.Dtos;

namespace ProjectNamePlaceholder.Application.SiteSettings;

public interface ISiteSettingService
{
    Task<IReadOnlyList<SiteSettingDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<SiteSettingDto?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);
    Task<SiteSettingDto> CreateOrUpdateAsync(SiteSettingDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(long id, CancellationToken cancellationToken = default);
}
