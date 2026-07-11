using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectNamePlaceholder.Api.Common;
using ProjectNamePlaceholder.Application.Permissions;
using ProjectNamePlaceholder.Application.Permissions.Dtos;
using ProjectNamePlaceholder.Application.Security;

namespace ProjectNamePlaceholder.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/permissions")]
[Authorize]
public sealed class PermissionController : ControllerBase
{
    private readonly IPermissionService _permissionService;

    public PermissionController(IPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    [HttpGet]
    [Authorize(Policy = Permissions.PermissionsRead)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PermissionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var permissions = await _permissionService.GetAllAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<PermissionDto>>.SuccessResponse(permissions, "Permissions retrieved successfully"));
    }

    [HttpGet("{id:long}")]
    [Authorize(Policy = Permissions.PermissionsRead)]
    [ProducesResponseType(typeof(ApiResponse<PermissionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
    {
        var permission = await _permissionService.GetByIdAsync(id, cancellationToken);
        if (permission is null)
        {
            return NotFound(ApiResponse.FailureResponse("Permission not found", StatusCodes.Status404NotFound));
        }

        return Ok(ApiResponse<PermissionDto>.SuccessResponse(permission, "Permission retrieved successfully"));
    }

    [HttpPost]
    [Authorize(Policy = Permissions.PermissionsCreate)]
    [ProducesResponseType(typeof(ApiResponse<PermissionDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreatePermissionRequestDto request, CancellationToken cancellationToken)
    {
        var permission = await _permissionService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = permission.Id }, ApiResponse<PermissionDto>.SuccessResponse(permission, "Permission created successfully", StatusCodes.Status201Created));
    }

    [HttpPut("{id:long}")]
    [Authorize(Policy = Permissions.PermissionsUpdate)]
    [ProducesResponseType(typeof(ApiResponse<PermissionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdatePermissionRequestDto request, CancellationToken cancellationToken)
    {
        var permission = await _permissionService.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponse<PermissionDto>.SuccessResponse(permission, "Permission updated successfully"));
    }

    [HttpDelete("{id:long}")]
    [Authorize(Policy = Permissions.PermissionsDelete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        await _permissionService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}