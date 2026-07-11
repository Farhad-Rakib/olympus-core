using ProjectNamePlaceholder.Application.Permissions.Dtos;

namespace ProjectNamePlaceholder.Application.Permissions;

public interface IPermissionService
{
    Task<IReadOnlyList<PermissionDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PermissionDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<PermissionDto> CreateAsync(CreatePermissionRequestDto request, CancellationToken cancellationToken = default);
    Task<PermissionDto> UpdateAsync(long id, UpdatePermissionRequestDto request, CancellationToken cancellationToken = default);
    Task DeleteAsync(long id, CancellationToken cancellationToken = default);
}