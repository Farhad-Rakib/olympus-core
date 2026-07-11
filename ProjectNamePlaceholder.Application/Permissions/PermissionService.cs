using ProjectNamePlaceholder.Application.Common.Interfaces;
using ProjectNamePlaceholder.Application.Permissions.Dtos;
using ProjectNamePlaceholder.Domain.Entities;

namespace ProjectNamePlaceholder.Application.Permissions;

public sealed class PermissionService : IPermissionService
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPermissionCache _permissionCache;

    // cache TTL for permissions
    private readonly TimeSpan CacheTtl;

    public PermissionService(IPermissionRepository permissionRepository, IUnitOfWork unitOfWork, IPermissionCache permissionCache, Microsoft.Extensions.Options.IOptions<ProjectNamePlaceholder.Application.Common.Configuration.CachingOptions> cachingOptions)
    {
        _permissionRepository = permissionRepository;
        _unitOfWork = unitOfWork;
        _permissionCache = permissionCache;
        CacheTtl = TimeSpan.FromMinutes(cachingOptions?.Value?.PermissionsTtlMinutes ?? 30);
    }

    public async Task<IReadOnlyList<PermissionDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        // Try cache first
        var cached = await _permissionCache.GetAllAsync(cancellationToken);
        if (cached != null && cached.Count > 0)
            return cached;

        var permissions = await _permissionRepository.GetAllAsync(cancellationToken);
        var result = permissions.Select(MapPermission).ToList();
        await _permissionCache.SetAllAsync(result, CacheTtl, cancellationToken);
        return result;
    }

    public async Task<PermissionDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var permission = await _permissionRepository.GetByIdAsync(id, cancellationToken);
        return permission is null ? null : MapPermission(permission);
    }

    public async Task<PermissionDto> CreateAsync(CreatePermissionRequestDto request, CancellationToken cancellationToken = default)
    {
        await EnsurePermissionNameIsAvailableAsync(request.Name, cancellationToken);

        var permission = new Permission(request.Name, request.Description);
        await _permissionRepository.AddAsync(permission, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // invalidate cache
        await _permissionCache.RemoveAllAsync(cancellationToken);

        return MapPermission(permission);
    }

    public async Task<PermissionDto> UpdateAsync(long id, UpdatePermissionRequestDto request, CancellationToken cancellationToken = default)
    {
        var permission = await _permissionRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("Permission not found.");

        if (!string.Equals(permission.Name, request.Name, StringComparison.OrdinalIgnoreCase))
        {
            await EnsurePermissionNameIsAvailableAsync(request.Name, cancellationToken, permission.Id);
        }

        permission.Update(request.Name, request.Description);
        _permissionRepository.Update(permission);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // invalidate cache
        await _permissionCache.RemoveAllAsync(cancellationToken);

        return MapPermission(permission);
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var permission = await _permissionRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("Permission not found.");

        _permissionRepository.Delete(permission);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // invalidate cache
        await _permissionCache.RemoveAllAsync(cancellationToken);
    }

    private async Task EnsurePermissionNameIsAvailableAsync(string name, CancellationToken cancellationToken, long? ignorePermissionId = null)
    {
        var existing = await _permissionRepository.GetByNameAsync(name, cancellationToken);
        if (existing is not null && existing.Id != ignorePermissionId)
        {
            throw new InvalidOperationException("Permission name already exists.");
        }
    }

    private static PermissionDto MapPermission(Permission permission)
    {
        return new PermissionDto(permission.Id, permission.Name, permission.Description);
    }
}