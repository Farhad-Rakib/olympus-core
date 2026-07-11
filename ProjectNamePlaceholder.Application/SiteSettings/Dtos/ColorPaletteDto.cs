namespace ProjectNamePlaceholder.Application.SiteSettings.Dtos;

public sealed record ColorPaletteDto(
    long? Id,
    string Key,
    Dictionary<string,string> Colors
);
