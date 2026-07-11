using Microsoft.EntityFrameworkCore;
using ProjectNamePlaceholder.Application.Common.Interfaces;
using ProjectNamePlaceholder.Domain.Entities;
using ProjectNamePlaceholder.Persistence.Context;

namespace ProjectNamePlaceholder.Persistence.Repositories;

public sealed class PermissionRepository : BaseRepository<Permission>, IPermissionRepository
{
    public PermissionRepository(ApplicationDbContext dbContext)
        : base(dbContext)
    {
    }

    public async Task<Permission?> GetByNameAsync(string permissionName, CancellationToken cancellationToken = default)
    {
        return await DbContext.Permissions
            .FirstOrDefaultAsync(permission => permission.Name == permissionName, cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetPermissionNamesForUserAsync(long userId, CancellationToken cancellationToken = default)
    {
        return await DbContext.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == userId)
            .SelectMany(ur => ur.Role.RolePermissions.Select(rp => rp.Permission.Name))
            .Distinct()
            .ToListAsync(cancellationToken);
    }
}
