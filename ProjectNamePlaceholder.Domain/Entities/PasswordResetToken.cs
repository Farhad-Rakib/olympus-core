namespace ProjectNamePlaceholder.Domain.Entities;

public sealed class PasswordResetToken : BaseEntity
{
    public long UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime? UsedAtUtc { get; private set; }

    public User User { get; private set; } = null!;

    private PasswordResetToken()
    {
    }

    public PasswordResetToken(long userId, string tokenHash, DateTime expiresAtUtc)
    {
        UserId = userId;
        TokenHash = !string.IsNullOrWhiteSpace(tokenHash)
            ? tokenHash
            : throw new ArgumentException("Token hash is required.", nameof(tokenHash));
        ExpiresAtUtc = expiresAtUtc;
    }

    public bool IsValid => UsedAtUtc is null && ExpiresAtUtc > DateTime.UtcNow;

    public void MarkAsUsed()
    {
        if (UsedAtUtc is not null)
        {
            return;
        }

        UsedAtUtc = DateTime.UtcNow;
    }
}
