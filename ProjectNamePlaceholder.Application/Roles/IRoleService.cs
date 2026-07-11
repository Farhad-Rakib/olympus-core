using ProjectNamePlaceholder.Application.Permissions.Dtos;
using ProjectNamePlaceholder.Application.Roles.Dtos;

namespace ProjectNamePlaceholder.Application.Roles;

public interface IRoleService
{
    Task<IReadOnlyList<RoleDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<RoleDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<RoleDto> CreateAsync(CreateRoleRequestDto request, CancellationToken cancellationToken = default);
    Task<RoleDto> UpdateAsync(long id, UpdateRoleRequestDto request, CancellationToken cancellationToken = default);
    Task DeleteAsync(long id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PermissionDto>> GetPermissionsAsync(long roleId, CancellationToken cancellationToken = default);
    Task<RoleDto> ReplacePermissionsAsync(long roleId, UpdateRolePermissionsRequestDto request, CancellationToken cancellationToken = default);
    Task<RoleDto> AddPermissionAsync(long roleId, long permissionId, CancellationToken cancellationToken = default);
    Task<RoleDto> RemovePermissionAsync(long roleId, long permissionId, CancellationToken cancellationToken = default);
}