using Microsoft.EntityFrameworkCore;
using ProjectNamePlaceholder.Application.Menu;
using ProjectNamePlaceholder.Domain.Entities;
using ProjectNamePlaceholder.Persistence.Context;

namespace ProjectNamePlaceholder.Persistence.Repositories
{
    public class MenuRepository : BaseRepository<Menu>, IMenuRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public MenuRepository(ApplicationDbContext dbContext)
            : base(dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<Menu>> GetRootMenusWithChildrenAsync(CancellationToken cancellationToken = default)
        {
            return await DbContext.Menus
                .AsNoTracking()
                .Include(m => m.Children)
                .Where(m => m.ParentMenuId == null)
                .ToListAsync(cancellationToken);
        }

        public override async Task<IReadOnlyList<Menu>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await DbContext.Menus
                .AsNoTracking()
                .Include(m => m.Children)
                .ToListAsync(cancellationToken);
        }

        public override async Task<Menu?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        {
            return await DbContext.Menus
                .Include(m => m.Children)
                .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        }

        public async Task<Menu> CreateAsync(Menu menu, CancellationToken cancellationToken = default)
        {
            await AddAsync(menu, cancellationToken);
            return menu;
        }

        public Task UpdateAsync(Menu menu, CancellationToken cancellationToken = default)
        {
            Update(menu);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Menu menu, CancellationToken cancellationToken = default)
        {
            Delete(menu);
            return Task.CompletedTask;
        }
    }
}
