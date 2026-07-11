using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectNamePlaceholder.Api.Common;
using ProjectNamePlaceholder.Application.Permissions.Dtos;
using ProjectNamePlaceholder.Application.Roles;
using ProjectNamePlaceholder.Application.Roles.Dtos;
using ProjectNamePlaceholder.Application.Security;

namespace ProjectNamePlaceholder.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/roles")]
[Authorize]
public sealed class RoleController : ControllerBase
{
    private readonly IRoleService _roleService;

    public RoleController(IRoleService roleService)
    {
        _roleService = roleService;
    }

    [HttpGet]
    [Authorize(Policy = Permissions.RolesRead)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<RoleDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var roles = await _roleService.GetAllAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<RoleDto>>.SuccessResponse(roles, "Roles retrieved successfully"));
    }

    [HttpGet("{id:long}")]
    [Authorize(Policy = Permissions.RolesRead)]
    [ProducesResponseType(typeof(ApiResponse<RoleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
    {
        var role = await _roleService.GetByIdAsync(id, cancellationToken);
        if (role is null)
        {
            return NotFound(ApiResponse.FailureResponse("Role not found", StatusCodes.Status404NotFound));
        }

        return Ok(ApiResponse<RoleDto>.SuccessResponse(role, "Role retrieved successfully"));
    }

    [HttpPost]
    [Authorize(Policy = Permissions.RolesCreate)]
    [ProducesResponseType(typeof(ApiResponse<RoleDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateRoleRequestDto request, CancellationToken cancellationToken)
    {
        var role = await _roleService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = role.Id }, ApiResponse<RoleDto>.SuccessResponse(role, "Role created successfully", StatusCodes.Status201Created));
    }

    [HttpPut("{id:long}")]
    [Authorize(Policy = Permissions.RolesUpdate)]
    [ProducesResponseType(typeof(ApiResponse<RoleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateRoleRequestDto request, CancellationToken cancellationToken)
    {
        var role = await _roleService.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponse<RoleDto>.SuccessResponse(role, "Role updated successfully"));
    }

    [HttpDelete("{id:long}")]
    [Authorize(Policy = Permissions.RolesDelete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        await _roleService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("{roleId:long}/permissions")]
    [Authorize(Policy = Permissions.RolePermissionsRead)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PermissionDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPermissions(long roleId, CancellationToken cancellationToken)
    {
        var permissions = await _roleService.GetPermissionsAsync(roleId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<PermissionDto>>.SuccessResponse(permissions, "Role permissions retrieved successfully"));
    }

    [HttpPut("{roleId:long}/permissions")]
    [Authorize(Policy = Permissions.RolePermissionsUpdate)]
    [ProducesResponseType(typeof(ApiResponse<RoleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ReplacePermissions(long roleId, [FromBody] UpdateRolePermissionsRequestDto request, CancellationToken cancellationToken)
    {
        var role = await _roleService.ReplacePermissionsAsync(roleId, request, cancellationToken);
        return Ok(ApiResponse<RoleDto>.SuccessResponse(role, "Role permissions updated successfully"));
    }

    [HttpPost("{roleId:long}/permissions/{permissionId:long}")]
    [Authorize(Policy = Permissions.RolePermissionsCreate)]
    [ProducesResponseType(typeof(ApiResponse<RoleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> AddPermission(long roleId, long permissionId, CancellationToken cancellationToken)
    {
        var role = await _roleService.AddPermissionAsync(roleId, permissionId, cancellationToken);
        return Ok(ApiResponse<RoleDto>.SuccessResponse(role, "Role permission added successfully"));
    }

    [HttpDelete("{roleId:long}/permissions/{permissionId:long}")]
    [Authorize(Policy = Permissions.RolePermissionsDelete)]
    [ProducesResponseType(typeof(ApiResponse<RoleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RemovePermission(long roleId, long permissionId, CancellationToken cancellationToken)
    {
        var role = await _roleService.RemovePermissionAsync(roleId, permissionId, cancellationToken);
        return Ok(ApiResponse<RoleDto>.SuccessResponse(role, "Role permission removed successfully"));
    }
}