using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectNamePlaceholder.Api.Common;
using ProjectNamePlaceholder.Application.SiteSettings;
using ProjectNamePlaceholder.Application.SiteSettings.Dtos;
using ProjectNamePlaceholder.Application.Security;

    

namespace ProjectNamePlaceholder.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public sealed class SiteSettingsController : ControllerBase
{
    private readonly ISiteSettingService _siteSettingService;

    public SiteSettingsController(ISiteSettingService siteSettingService)
    {
        _siteSettingService = siteSettingService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<SiteSettingDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var settings = await _siteSettingService.GetAllAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<SiteSettingDto>>.SuccessResponse(settings, "Site settings retrieved successfully"));
    }

    [HttpGet("{key}")]
    [ProducesResponseType(typeof(ApiResponse<SiteSettingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByKey(string key, CancellationToken cancellationToken)
    {
        var setting = await _siteSettingService.GetByKeyAsync(key, cancellationToken);
        if (setting is null)
            return NotFound(ApiResponse.FailureResponse("Setting not found", StatusCodes.Status404NotFound));

        return Ok(ApiResponse<SiteSettingDto>.SuccessResponse(setting, "Site setting retrieved successfully"));
    }

    [HttpGet("palette/{key}")]
    [Authorize(Policy = Permissions.SiteSettingsRead)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPalette(string key, CancellationToken cancellationToken)
    {
        var setting = await _siteSettingService.GetByKeyAsync(key, cancellationToken);
        if (setting is null) return NotFound(ApiResponse.FailureResponse("Palette not found", StatusCodes.Status404NotFound));

        try
        {
            var parsed = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(setting.Value) ?? new Dictionary<string, string>();
            return Ok(ApiResponse<object>.SuccessResponse(parsed, "Palette retrieved"));
        }
        catch
        {
            return Ok(ApiResponse<object>.SuccessResponse(setting.Value, "Palette retrieved as raw string"));
        }
    }

    [HttpPost("palette")]
    [Authorize(Policy = Permissions.SiteSettingsCreate)]
    [ProducesResponseType(typeof(ApiResponse<SiteSettingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateOrUpdatePalette([FromBody] ColorPaletteDto dto, CancellationToken cancellationToken)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(dto.Colors);
        var settingDto = new SiteSettingDto(dto.Id ?? 0, dto.Key, json, "Color palette");
        var result = await _siteSettingService.CreateOrUpdateAsync(settingDto, cancellationToken);
        return Ok(ApiResponse<SiteSettingDto>.SuccessResponse(result, "Palette saved"));
    }

    [HttpDelete("palette/{id:long}")]
    [Authorize(Policy = Permissions.SiteSettingsDelete)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeletePalette(long id, CancellationToken cancellationToken)
    {
        await _siteSettingService.DeleteAsync(id, cancellationToken);
        return Ok(ApiResponse.SuccessResponse("Palette deleted"));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<SiteSettingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateOrUpdate([FromBody] SiteSettingDto dto, CancellationToken cancellationToken)
    {
        var result = await _siteSettingService.CreateOrUpdateAsync(dto, cancellationToken);
        return Ok(ApiResponse<SiteSettingDto>.SuccessResponse(result, "Site setting saved successfully", StatusCodes.Status200OK));
    }

    [HttpDelete("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        await _siteSettingService.DeleteAsync(id, cancellationToken);
        return Ok(ApiResponse.SuccessResponse("Site setting deleted successfully"));
    }
}

