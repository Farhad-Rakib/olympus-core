namespace ProjectNamePlaceholder.Application.SiteSettings.Dtos;

public sealed record SiteSettingDto(
    long Id,
    string Key,
    string Value,
    string? Description
);
