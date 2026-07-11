using ProjectNamePlaceholder.Application.Permissions.Dtos;

namespace ProjectNamePlaceholder.Application.Permissions;

public interface IPermissionCache
{
    Task<IReadOnlyList<PermissionDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task SetAllAsync(IReadOnlyList<PermissionDto> permissions, TimeSpan ttl, CancellationToken cancellationToken = default);
    Task RemoveAllAsync(CancellationToken cancellationToken = default);
}
