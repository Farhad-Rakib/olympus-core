using ProjectNamePlaceholder.Application.Common.Interfaces;
using ProjectNamePlaceholder.Application.Common.Exceptions;
using System.Text.Json;
using ProjectNamePlaceholder.Application.SiteSettings.Dtos;
using ProjectNamePlaceholder.Domain.Entities;

namespace ProjectNamePlaceholder.Application.SiteSettings;

public sealed class SiteSettingService : ISiteSettingService
{
    private readonly ISiteSettingRepository _siteSettingRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SiteSettingService(ISiteSettingRepository siteSettingRepository, IUnitOfWork unitOfWork)
    {
        _siteSettingRepository = siteSettingRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<SiteSettingDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _siteSettingRepository.GetAllAsync(cancellationToken);
        return settings.Select(MapToDto).ToList();
    }

    public async Task<SiteSettingDto?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        var setting = await _siteSettingRepository.GetByKeyAsync(key, cancellationToken);
        return setting is null ? null : MapToDto(setting);
    }

    public async Task<SiteSettingDto> CreateOrUpdateAsync(SiteSettingDto dto, CancellationToken cancellationToken = default)
    {
        // Validate palette JSON when saving palettes
        if (!string.IsNullOrWhiteSpace(dto.Key) && dto.Key.Contains("palette", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(dto.Value ?? string.Empty);
                if (parsed is null)
                {
                    throw new AppException("Palette JSON is invalid or empty", 400);
                }
            }
            catch (JsonException)
            {
                throw new AppException("Palette JSON is invalid", 400);
            }
        }

        var existing = await _siteSettingRepository.GetByKeyAsync(dto.Key, cancellationToken);
        if (existing is null)
        {
            var created = new SiteSetting
            {
                Id =  dto.Id,
                Key = dto.Key,
                Value = dto.Value,
                Description = dto.Description
            };

            await _siteSettingRepository.AddAsync(created, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return MapToDto(created);
        }

        existing.Value = dto.Value;
        existing.Description = dto.Description;

        _siteSettingRepository.Update(existing);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(existing);
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var setting = await _siteSettingRepository.GetByIdAsync(id, cancellationToken);
        if (setting is null)
        {
            return;
        }

        _siteSettingRepository.Delete(setting);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static SiteSettingDto MapToDto(SiteSetting setting)
        => new(setting.Id, setting.Key, setting.Value, setting.Description);
}
