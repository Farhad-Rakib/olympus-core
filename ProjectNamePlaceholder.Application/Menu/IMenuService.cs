
using System.Security.Claims;

namespace ProjectNamePlaceholder.Application.Menu;

public interface IMenuService
{
    Task<List<MenuItemDto>> GetMenuForUserAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default);
    Task<List<MenuDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<MenuDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<MenuDto> CreateAsync(CreateMenuRequestDto request, CancellationToken cancellationToken = default);
    Task UpdateAsync(long id, UpdateMenuRequestDto request, CancellationToken cancellationToken = default);
    Task DeleteAsync(long id, CancellationToken cancellationToken = default);
}
