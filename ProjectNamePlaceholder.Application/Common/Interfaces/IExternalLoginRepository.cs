using ProjectNamePlaceholder.Domain.Entities;

namespace ProjectNamePlaceholder.Application.Common.Interfaces;

public interface IExternalLoginRepository : IRepository<ExternalLogin>
{
    Task<ExternalLogin?> FindAsync(string provider, string providerKey, CancellationToken cancellationToken = default);
}
