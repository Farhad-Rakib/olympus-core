namespace ProjectNamePlaceholder.Domain.Entities;

public sealed class RefreshToken : BaseEntity
{
    public long UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }
    public string? ReplacedByTokenHash { get; private set; }

    public User User { get; private set; } = null!;

    private RefreshToken()
    {
    }

    public RefreshToken(long userId, string tokenHash, DateTime expiresAtUtc)
    {
        UserId = userId;
        TokenHash = !string.IsNullOrWhiteSpace(tokenHash)
            ? tokenHash
            : throw new ArgumentException("Token hash is required.", nameof(tokenHash));
        ExpiresAtUtc = expiresAtUtc;
    }

    public bool IsActive => RevokedAtUtc is null && ExpiresAtUtc > DateTime.UtcNow;

    public void Revoke(string? replacedByTokenHash = null)
    {
        if (RevokedAtUtc is not null)
        {
            return;
        }

        RevokedAtUtc = DateTime.UtcNow;
        ReplacedByTokenHash = replacedByTokenHash;
    }
}
