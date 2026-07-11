using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectNamePlaceholder.Api.Common;
using ProjectNamePlaceholder.Application.Security;
using ProjectNamePlaceholder.Api.Startup;

namespace ProjectNamePlaceholder.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public sealed class SystemController : ControllerBase
{
    private readonly EndpointDataSource _endpointDataSource;
    private readonly RedisCacheAdminService _redisCacheAdminService;
    private readonly DistributedCacheAdminService _distributedCacheAdminService;

    public SystemController(
        EndpointDataSource endpointDataSource,
        RedisCacheAdminService redisCacheAdminService,
        DistributedCacheAdminService distributedCacheAdminService)
    {
        _endpointDataSource = endpointDataSource;
        _redisCacheAdminService = redisCacheAdminService;
        _distributedCacheAdminService = distributedCacheAdminService;
    }

    [HttpGet("endpoints")]
    [Authorize(Policy = Permissions.SystemEndpointsRead)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<EndpointInfoResponse>>), StatusCodes.Status200OK)]
    public IActionResult GetAllEndpoints()
    {
        var endpoints = _endpointDataSource.Endpoints
            .OfType<RouteEndpoint>()
            .Select(endpoint => new EndpointInfoResponse(
                endpoint.DisplayName ?? string.Empty,
                endpoint.RoutePattern.RawText ?? string.Empty,
                endpoint.Metadata
                    .OfType<HttpMethodMetadata>()
                    .SelectMany(metadata => metadata.HttpMethods)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(method => method)
                    .ToList()))
            .OrderBy(x => x.Route)
            .ThenBy(x => x.DisplayName)
            .ToList();

        return Ok(ApiResponse<IReadOnlyList<EndpointInfoResponse>>.SuccessResponse(endpoints, "Endpoints retrieved successfully"));
    }

    [HttpGet("cache/keys")]
    [Authorize(Policy = Permissions.SystemCacheRead)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<RedisCacheEntryResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetCacheEntries([FromQuery] string? pattern, CancellationToken cancellationToken)
    {
        try
        {
            var entries = await _redisCacheAdminService.GetEntriesAsync(pattern, cancellationToken);
            return Ok(ApiResponse<IReadOnlyList<RedisCacheEntryResponse>>.SuccessResponse(
                entries.Select(entry => new RedisCacheEntryResponse(entry.Key, entry.Value, entry.Fields, entry.TimeToLiveSeconds)).ToList(),
                "Cache entries retrieved successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, ApiResponse.FailureResponse(ex.Message, StatusCodes.Status503ServiceUnavailable));
        }
    }

    [HttpDelete("cache/flush")]
    [Authorize(Policy = Permissions.SystemCacheFlush)]
    [ProducesResponseType(typeof(ApiResponse<RedisCacheFlushResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> FlushCache(CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await _redisCacheAdminService.FlushAsync(cancellationToken);
            return Ok(ApiResponse<RedisCacheFlushResponse>.SuccessResponse(
                new RedisCacheFlushResponse(deleted),
                "Cache flushed successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, ApiResponse.FailureResponse(ex.Message, StatusCodes.Status503ServiceUnavailable));
        }
    }

    [HttpGet("cache/distributed/keys")]
    [Authorize(Policy = Permissions.SystemCacheRead)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<DistributedCacheEntryResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDistributedCacheEntries([FromQuery] string? pattern, CancellationToken cancellationToken)
    {
        var entries = await _distributedCacheAdminService.GetEntriesAsync(pattern, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<DistributedCacheEntryResponse>>.SuccessResponse(
            entries.Select(entry => new DistributedCacheEntryResponse(entry.Key, entry.Value)).ToList(),
            "Distributed cache entries retrieved successfully"));
    }

    [HttpDelete("cache/distributed/flush")]
    [Authorize(Policy = Permissions.SystemCacheFlush)]
    [ProducesResponseType(typeof(ApiResponse<DistributedCacheFlushResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> FlushDistributedCache(CancellationToken cancellationToken)
    {
        var deleted = await _distributedCacheAdminService.FlushAsync(cancellationToken);
        return Ok(ApiResponse<DistributedCacheFlushResponse>.SuccessResponse(
            new DistributedCacheFlushResponse(deleted),
            "Distributed cache flushed successfully"));
    }

    // Palette endpoints moved to SiteSettingsController

    public sealed record EndpointInfoResponse(string DisplayName, string Route, IReadOnlyList<string> Methods);
    public sealed record RedisCacheEntryResponse(string Key, string? Value, IReadOnlyDictionary<string, string> Fields, double? TimeToLiveSeconds);
    public sealed record RedisCacheFlushResponse(long DeletedCount);
    public sealed record DistributedCacheEntryResponse(string Key, string? Value);
    public sealed record DistributedCacheFlushResponse(long DeletedCount);
}

