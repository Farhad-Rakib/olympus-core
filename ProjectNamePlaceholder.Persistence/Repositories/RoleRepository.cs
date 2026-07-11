using Microsoft.EntityFrameworkCore;
using ProjectNamePlaceholder.Application.Common.Interfaces;
using ProjectNamePlaceholder.Domain.Entities;
using ProjectNamePlaceholder.Persistence.Context;

namespace ProjectNamePlaceholder.Persistence.Repositories;

public sealed class RoleRepository : BaseRepository<Role>, IRoleRepository
{
    public RoleRepository(ApplicationDbContext dbContext)
        : base(dbContext)
    {
    }

    public override async Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await DbContext.Roles
            .AsNoTracking()
            .Include(role => role.RolePermissions)
                .ThenInclude(rolePermission => rolePermission.Permission)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Role>> GetByNamesAsync(IEnumerable<string> roleNames, CancellationToken cancellationToken = default)
    {
        var normalized = roleNames.Select(r => r.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);

        return await DbContext.Roles
            .Where(r => normalized.Contains(r.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<Role?> GetByNameAsync(string roleName, CancellationToken cancellationToken = default)
    {
        return await DbContext.Roles
            .Include(role => role.RolePermissions)
                .ThenInclude(rolePermission => rolePermission.Permission)
            .FirstOrDefaultAsync(role => role.Name == roleName, cancellationToken);
    }

    public async Task<Role?> GetByIdWithPermissionsAsync(long id, CancellationToken cancellationToken = default)
    {
        return await DbContext.Roles
            .Include(role => role.RolePermissions)
                .ThenInclude(rolePermission => rolePermission.Permission)
            .FirstOrDefaultAsync(role => role.Id == id, cancellationToken);
    }
}
