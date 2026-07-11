using ProjectNamePlaceholder.Domain.Entities;

namespace ProjectNamePlaceholder.Application.Common.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByIdWithRolesAsync(long id, CancellationToken cancellationToken = default);
}
