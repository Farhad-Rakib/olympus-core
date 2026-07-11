using ProjectNamePlaceholder.Application.Common.Interfaces;
using ProjectNamePlaceholder.Application.Permissions.Dtos;
using ProjectNamePlaceholder.Application.Roles.Dtos;
using ProjectNamePlaceholder.Domain.Entities;

namespace ProjectNamePlaceholder.Application.Roles;

public sealed class RoleService : IRoleService
{
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IUnitOfWork _unitOfWork;
        private readonly ProjectNamePlaceholder.Application.Common.Interfaces.IAppCache _cache;

        private readonly TimeSpan RoleCacheTtl;

    public RoleService(
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        IUnitOfWork unitOfWork,
        ProjectNamePlaceholder.Application.Common.Interfaces.IAppCache cache,
        Microsoft.Extensions.Options.IOptions<ProjectNamePlaceholder.Application.Common.Configuration.CachingOptions> cachingOptions)
    {
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _unitOfWork = unitOfWork;
        _cache = cache;
        RoleCacheTtl = TimeSpan.FromMinutes(cachingOptions?.Value?.RolesTtlMinutes ?? 30);
    }

    public async Task<IReadOnlyList<RoleDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var cached = await _cache.GetAsync<List<RoleDto>>("roles:all", cancellationToken);
        if (cached is not null && cached.Count > 0)
            return cached;

        var roles = await _roleRepository.GetAllAsync(cancellationToken);
        var result = roles.Select(MapRole).ToList();
        await _cache.SetAsync("roles:all", result, RoleCacheTtl, cancellationToken);
        return result;
    }

    public async Task<RoleDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var cached = await _cache.GetAsync<RoleDto>($"roles:{id}", cancellationToken);
        if (cached is not null)
            return cached;

        var role = await _roleRepository.GetByIdWithPermissionsAsync(id, cancellationToken);
        if (role is null) return null;
        var dto = MapRole(role);
        await _cache.SetAsync($"roles:{id}", dto, RoleCacheTtl, cancellationToken);
        return dto;
    }

    public async Task<RoleDto> CreateAsync(CreateRoleRequestDto request, CancellationToken cancellationToken = default)
    {
        await EnsureRoleNameIsAvailableAsync(request.Name, cancellationToken);

        var role = new Role(request.Name, request.Description);
        await _roleRepository.AddAsync(role, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // invalidate roles list cache
        await _cache.RemoveAsync("roles:all", cancellationToken);

        return MapRole(role);
    }

    public async Task<RoleDto> UpdateAsync(long id, UpdateRoleRequestDto request, CancellationToken cancellationToken = default)
    {
        var role = await _roleRepository.GetByIdWithPermissionsAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("Role not found.");

        if (!string.Equals(role.Name, request.Name, StringComparison.OrdinalIgnoreCase))
        {
            await EnsureRoleNameIsAvailableAsync(request.Name, cancellationToken, role.Id);
        }

        role.Update(request.Name, request.Description);
        _roleRepository.Update(role);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // invalidate caches
        await _cache.RemoveAsync("roles:all", cancellationToken);
        await _cache.RemoveAsync($"roles:{id}", cancellationToken);

        return MapRole(await _roleRepository.GetByIdWithPermissionsAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("Role not found after update."));
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var role = await _roleRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("Role not found.");

        _roleRepository.Delete(role);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _cache.RemoveAsync("roles:all", cancellationToken);
        await _cache.RemoveAsync($"roles:{id}", cancellationToken);
    }

    public async Task<IReadOnlyList<PermissionDto>> GetPermissionsAsync(long roleId, CancellationToken cancellationToken = default)
    {
        var cached = await _cache.GetAsync<List<PermissionDto>>($"role:{roleId}:permissions", cancellationToken);
        if (cached is not null && cached.Count > 0)
            return cached;

        var role = await _roleRepository.GetByIdWithPermissionsAsync(roleId, cancellationToken)
            ?? throw new InvalidOperationException("Role not found.");

        var result = role.RolePermissions
            .Select(rolePermission => new PermissionDto(
                rolePermission.Permission.Id,
                rolePermission.Permission.Name,
                rolePermission.Permission.Description))
            .ToList();

        await _cache.SetAsync($"role:{roleId}:permissions", result, RoleCacheTtl, cancellationToken);

        return result;
    }

    public async Task<RoleDto> ReplacePermissionsAsync(long roleId, UpdateRolePermissionsRequestDto request, CancellationToken cancellationToken = default)
    {
        var role = await _roleRepository.GetByIdWithPermissionsAsync(roleId, cancellationToken)
            ?? throw new InvalidOperationException("Role not found.");

        var permissions = await GetPermissionsByIdsAsync(request.PermissionIds, cancellationToken);
        role.RolePermissions.Clear();

        foreach (var permission in permissions)
        {
            role.RolePermissions.Add(new RolePermission
            {
                RoleId = role.Id,
                PermissionId = permission.Id,
                Role = role,
                Permission = permission
            });
        }

        _roleRepository.Update(role);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        // invalidate caches for this role
        await _cache.RemoveAsync("roles:all", cancellationToken);
        await _cache.RemoveAsync($"roles:{roleId}", cancellationToken);
        await _cache.RemoveAsync($"role:{roleId}:permissions", cancellationToken);

        return MapRole(await _roleRepository.GetByIdWithPermissionsAsync(roleId, cancellationToken)
            ?? throw new InvalidOperationException("Role not found after update."));
    }

    public async Task<RoleDto> AddPermissionAsync(long roleId, long permissionId, CancellationToken cancellationToken = default)
    {
        var role = await _roleRepository.GetByIdWithPermissionsAsync(roleId, cancellationToken)
            ?? throw new InvalidOperationException("Role not found.");

        var permission = await GetPermissionByIdAsync(permissionId, cancellationToken);
        if (role.RolePermissions.Any(x => x.PermissionId == permission.Id))
        {
            return MapRole(role);
        }

        role.RolePermissions.Add(new RolePermission
        {
            RoleId = role.Id,
            PermissionId = permission.Id,
            Role = role,
            Permission = permission
        });

        _roleRepository.Update(role);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _cache.RemoveAsync("roles:all", cancellationToken);
        await _cache.RemoveAsync($"roles:{roleId}", cancellationToken);
        await _cache.RemoveAsync($"role:{roleId}:permissions", cancellationToken);

        return MapRole(await _roleRepository.GetByIdWithPermissionsAsync(roleId, cancellationToken)
            ?? throw new InvalidOperationException("Role not found after update."));
    }

    public async Task<RoleDto> RemovePermissionAsync(long roleId, long permissionId, CancellationToken cancellationToken = default)
    {
        var role = await _roleRepository.GetByIdWithPermissionsAsync(roleId, cancellationToken)
            ?? throw new InvalidOperationException("Role not found.");

        var permissionLink = role.RolePermissions.FirstOrDefault(x => x.PermissionId == permissionId)
            ?? throw new InvalidOperationException("Role permission not found.");

        role.RolePermissions.Remove(permissionLink);
        _roleRepository.Update(role);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _cache.RemoveAsync("roles:all", cancellationToken);
        await _cache.RemoveAsync($"roles:{roleId}", cancellationToken);
        await _cache.RemoveAsync($"role:{roleId}:permissions", cancellationToken);

        return MapRole(await _roleRepository.GetByIdWithPermissionsAsync(roleId, cancellationToken)
            ?? throw new InvalidOperationException("Role not found after update."));
    }

    private async Task EnsureRoleNameIsAvailableAsync(string name, CancellationToken cancellationToken, long? ignoreRoleId = null)
    {
        var existing = await _roleRepository.GetByNameAsync(name, cancellationToken);
        if (existing is not null && existing.Id != ignoreRoleId)
        {
            throw new InvalidOperationException("Role name already exists.");
        }
    }

    private async Task<Permission> GetPermissionByIdAsync(long permissionId, CancellationToken cancellationToken)
    {
        return await _permissionRepository.GetByIdAsync(permissionId, cancellationToken)
            ?? throw new InvalidOperationException("Permission not found.");
    }

    private async Task<IReadOnlyList<Permission>> GetPermissionsByIdsAsync(IEnumerable<long> permissionIds, CancellationToken cancellationToken)
    {
        var distinctIds = permissionIds.Distinct().ToArray();
        var permissions = new List<Permission>(distinctIds.Length);

        foreach (var permissionId in distinctIds)
        {
            permissions.Add(await GetPermissionByIdAsync(permissionId, cancellationToken));
        }

        return permissions;
    }

    private static RoleDto MapRole(Role role)
    {
        return new RoleDto(
            role.Id,
            role.Name,
            role.Description,
            role.RolePermissions
                .Select(rolePermission => rolePermission.Permission.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList());
    }
}