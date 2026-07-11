using System.Security.Claims;

namespace ProjectNamePlaceholder.Application.Menu
{
    public class MenuService : IMenuService
    {
        private readonly IMenuRepository _menuRepository;
        private readonly ProjectNamePlaceholder.Application.Common.Interfaces.IAppCache _cache;

        private readonly TimeSpan MenuCacheTtl;

        public MenuService(IMenuRepository menuRepository, ProjectNamePlaceholder.Application.Common.Interfaces.IAppCache cache, Microsoft.Extensions.Options.IOptions<ProjectNamePlaceholder.Application.Common.Configuration.CachingOptions> cachingOptions)
        {
            _menuRepository = menuRepository;
            _cache = cache;
            MenuCacheTtl = TimeSpan.FromMinutes(cachingOptions?.Value?.MenusTtlMinutes ?? 30);
        }

        public async Task<List<MenuItemDto>> GetMenuForUserAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default)
        {
            var userPermissions = user.Claims
                .Where(c => c.Type == "permission")
                .Select(c => c.Value)
                .ToHashSet();

            // Try cached tree first
            var cached = await _cache.GetAsync<List<MenuDto>>("menus:tree", cancellationToken);
            List<Domain.Entities.Menu> allMenus;
            if (cached is not null && cached.Count > 0)
            {
                // Convert cached DTOs back to domain-like structure for filtering
                allMenus = cached.Select(d => new Domain.Entities.Menu
                {
                    Id = d.Id,
                    Title = d.Title,
                    Url = d.Url,
                    Icon = d.Icon,
                    RequiredPermission = d.RequiredPermission,
                    ParentMenuId = d.ParentMenuId,
                    Children = d.Children != null ? d.Children.Select(c => MapDtoToEntity(c)).ToList() : new List<Domain.Entities.Menu>()
                }).ToList();
            }
            else
            {
                allMenus = await _menuRepository.GetRootMenusWithChildrenAsync(cancellationToken);
                // Cache DTO representation
                var dtoTree = allMenus.Select(MapToDto).ToList();
                await _cache.SetAsync("menus:tree", dtoTree, MenuCacheTtl, cancellationToken);
            }

            return FilterMenuItems(allMenus, userPermissions);
        }

        private List<MenuItemDto> FilterMenuItems(List<Domain.Entities.Menu> items, HashSet<string> userPermissions)
        {
            return items
                .Where(item => string.IsNullOrEmpty(item.RequiredPermission) || userPermissions.Contains(item.RequiredPermission))
                .Select(item => new MenuItemDto
                {
                    Title = item.Title,
                    Url = item.Url,
                    Icon = item.Icon,
                    RequiredPermission = item.RequiredPermission,
                    Children = item.Children != null ? FilterMenuItems(item.Children, userPermissions) : null
                })
                .ToList();
        }

        public async Task<List<MenuDto>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var cached = await _cache.GetAsync<List<MenuDto>>("menus:all", cancellationToken);
            if (cached is not null && cached.Count > 0)
                return cached;

            var all = await _menuRepository.GetAllAsync(cancellationToken);
            var result = all.Select(MapToDto).ToList();
            await _cache.SetAsync("menus:all", result, MenuCacheTtl, cancellationToken);
            return result;
        }

        public async Task<MenuDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        {
            var menu = await _menuRepository.GetByIdAsync(id, cancellationToken);
            return menu == null ? null : MapToDto(menu);
        }

        public async Task<MenuDto> CreateAsync(CreateMenuRequestDto request, CancellationToken cancellationToken = default)
        {
            var entity = new Domain.Entities.Menu
            {
                Title = request.Title,
                Url = request.Url,
                Icon = request.Icon,
                RequiredPermission = request.RequiredPermission,
                ParentMenuId = request.ParentMenuId
            };

            var created = await _menuRepository.CreateAsync(entity, cancellationToken);
            // Invalidate caches
            await _cache.RemoveAsync("menus:tree", cancellationToken);
            await _cache.RemoveAsync("menus:all", cancellationToken);
            return MapToDto(created);
        }

        public async Task UpdateAsync(long id, UpdateMenuRequestDto request, CancellationToken cancellationToken = default)
        {
            var menu = await _menuRepository.GetByIdAsync(id, cancellationToken);
            if (menu == null) throw new KeyNotFoundException("Menu not found");

            menu.Title = request.Title;
            menu.Url = request.Url;
            menu.Icon = request.Icon;
            menu.RequiredPermission = request.RequiredPermission;
            menu.ParentMenuId = request.ParentMenuId;

            await _menuRepository.UpdateAsync(menu, cancellationToken);
            await _cache.RemoveAsync("menus:tree", cancellationToken);
            await _cache.RemoveAsync("menus:all", cancellationToken);
        }

        public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
        {
            var menu = await _menuRepository.GetByIdAsync(id, cancellationToken);
            if (menu == null) throw new KeyNotFoundException("Menu not found");

            await _menuRepository.DeleteAsync(menu, cancellationToken);
            await _cache.RemoveAsync("menus:tree", cancellationToken);
            await _cache.RemoveAsync("menus:all", cancellationToken);
        }

        private MenuDto MapToDto(Domain.Entities.Menu m)
        {
            return new MenuDto
            {
                Id = m.Id,
                Title = m.Title,
                Url = m.Url,
                Icon = m.Icon,
                RequiredPermission = m.RequiredPermission,
                ParentMenuId = m.ParentMenuId,
                Children = m.Children?.Select(MapToDto).ToList()
            };
        }

        private static Domain.Entities.Menu MapDtoToEntity(MenuDto d)
        {
            return new Domain.Entities.Menu
            {
                Id = d.Id,
                Title = d.Title,
                Url = d.Url,
                Icon = d.Icon,
                RequiredPermission = d.RequiredPermission,
                ParentMenuId = d.ParentMenuId,
                Children = d.Children?.Select(MapDtoToEntity).ToList() ?? new List<Domain.Entities.Menu>()
            };
        }
    }
}
