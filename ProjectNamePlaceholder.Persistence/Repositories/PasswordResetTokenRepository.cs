using Microsoft.EntityFrameworkCore;
using ProjectNamePlaceholder.Application.Common.Interfaces;
using ProjectNamePlaceholder.Domain.Entities;
using ProjectNamePlaceholder.Persistence.Context;

namespace ProjectNamePlaceholder.Persistence.Repositories;

public sealed class PasswordResetTokenRepository : BaseRepository<PasswordResetToken>, IPasswordResetTokenRepository
{
    public PasswordResetTokenRepository(ApplicationDbContext dbContext)
        : base(dbContext)
    {
    }

    public async Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<PasswordResetToken>()
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);
    }

    public async Task<PasswordResetToken?> GetLatestByUserIdAsync(long userId, CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<PasswordResetToken>()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
