using ProjectNamePlaceholder.Application.Common.Interfaces;
using ProjectNamePlaceholder.Application.Security;
using ProjectNamePlaceholder.Domain.Entities;

namespace ProjectNamePlaceholder.Api.Startup;

public sealed class PermissionSyncService
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PermissionSyncService> _logger;

    public PermissionSyncService(IPermissionRepository permissionRepository, IUnitOfWork unitOfWork, ILogger<PermissionSyncService> logger)
    {
        _permissionRepository = permissionRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task SyncAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var existing = await _permissionRepository.GetAllAsync(cancellationToken);
            var existingNames = new HashSet<string>(existing.Select(p => p.Name), StringComparer.OrdinalIgnoreCase);

            var toAdd = new List<Permission>();
            foreach (var name in Permissions.All)
            {
                if (!existingNames.Contains(name))
                {
                    toAdd.Add(new Permission(name, string.Empty));
                }
            }

            if (toAdd.Count > 0)
            {
                _logger.LogInformation("Syncing {Count} new permissions to database", toAdd.Count);
                foreach (var p in toAdd)
                {
                    await _permissionRepository.AddAsync(p, cancellationToken);
                }
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            else
            {
                _logger.LogDebug("Permissions already in sync");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync permissions");
            throw;
        }
    }
}
