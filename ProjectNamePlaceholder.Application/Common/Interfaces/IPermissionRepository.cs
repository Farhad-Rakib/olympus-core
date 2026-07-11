using ProjectNamePlaceholder.Domain.Entities;

namespace ProjectNamePlaceholder.Application.Common.Interfaces;

public interface IPermissionRepository : IRepository<Permission>
{
    Task<IReadOnlyList<string>> GetPermissionNamesForUserAsync(long userId, CancellationToken cancellationToken = default);
    Task<Permission?> GetByNameAsync(string permissionName, CancellationToken cancellationToken = default);
}
