using Microsoft.EntityFrameworkCore;
using ProjectNamePlaceholder.Application.Common.Interfaces;
using ProjectNamePlaceholder.Domain.Entities;
using ProjectNamePlaceholder.Persistence.Context;

namespace ProjectNamePlaceholder.Persistence.Repositories;

public class ExternalLoginRepository : BaseRepository<ExternalLogin>, IExternalLoginRepository
{
    public ExternalLoginRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
    }

    public Task<ExternalLogin?> FindAsync(string provider, string providerKey, CancellationToken cancellationToken = default)
    {
        return DbSet.FirstOrDefaultAsync(x => x.Provider == provider && x.ProviderKey == providerKey, cancellationToken);
    }
}
