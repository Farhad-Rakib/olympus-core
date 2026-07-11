using Microsoft.Extensions.Logging;
using ProjectNamePlaceholder.Application.Menu;
using ProjectNamePlaceholder.Application.Common.Interfaces;

namespace ProjectNamePlaceholder.Api.Startup;

public sealed class MenuPermissionValidator
{
    private readonly IMenuRepository _menuRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly ILogger<MenuPermissionValidator> _logger;

    public MenuPermissionValidator(IMenuRepository menuRepository, IPermissionRepository permissionRepository, ILogger<MenuPermissionValidator> logger)
    {
        _menuRepository = menuRepository;
        _permissionRepository = permissionRepository;
        _logger = logger;
    }

    /// <summary>
    /// Validate that each menu.RequiredPermission (if non-null) exists in the permissions table.
    /// If strict==true, throws InvalidOperationException on first missing permission; otherwise logs warnings.
    /// </summary>
    public async Task ValidateAsync(bool strict = false, CancellationToken cancellationToken = default)
    {
        var menus = await _menuRepository.GetAllAsync(cancellationToken);
        var missing = new List<string>();

        foreach (var menu in menus)
        {
            if (string.IsNullOrWhiteSpace(menu.RequiredPermission)) continue;

            var permission = await _permissionRepository.GetByNameAsync(menu.RequiredPermission, cancellationToken);
            if (permission is null)
            {
                missing.Add(menu.RequiredPermission);
                _logger.LogWarning("Menu '{Title}' references missing permission '{Permission}'", menu.Title, menu.RequiredPermission);
            }
        }

        if (missing.Any())
        {
            var message = $"Menu permission validation failed. Missing permissions: {string.Join(',', missing.Distinct())}";
            if (strict)
            {
                throw new InvalidOperationException(message);
            }

            _logger.LogWarning(message);
        }
        else
        {
            _logger.LogDebug("Menu permission validation passed.");
        }
    }
}
