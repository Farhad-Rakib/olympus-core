using ProjectNamePlaceholder.Domain.Entities;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectNamePlaceholder.Application.Menu
{
    public interface IMenuRepository
    {
        Task<List<ProjectNamePlaceholder.Domain.Entities.Menu>> GetRootMenusWithChildrenAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ProjectNamePlaceholder.Domain.Entities.Menu>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<ProjectNamePlaceholder.Domain.Entities.Menu?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
        Task<ProjectNamePlaceholder.Domain.Entities.Menu> CreateAsync(ProjectNamePlaceholder.Domain.Entities.Menu menu, CancellationToken cancellationToken = default);
        Task UpdateAsync(ProjectNamePlaceholder.Domain.Entities.Menu menu, CancellationToken cancellationToken = default);
        Task DeleteAsync(ProjectNamePlaceholder.Domain.Entities.Menu menu, CancellationToken cancellationToken = default);
    }
}
