using ProjectNamePlaceholder.Domain.ValueObjects;

namespace ProjectNamePlaceholder.Domain.Entities;

public sealed class User : BaseEntity
{
    public string FullName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    public string? ProfileImageUrl { get; private set; }

    public ICollection<UserRole> UserRoles { get; private set; } = new List<UserRole>();
    public ICollection<RefreshToken> RefreshTokens { get; private set; } = new List<RefreshToken>();

    private User()
    {
    }

    public User(string fullName, string email, string passwordHash)
    {
        FullName = !string.IsNullOrWhiteSpace(fullName)
            ? fullName.Trim()
            : throw new ArgumentException("Full name is required.", nameof(fullName));

        Email = EmailAddress.Create(email).Value;
        PasswordHash = !string.IsNullOrWhiteSpace(passwordHash)
            ? passwordHash
            : throw new ArgumentException("Password hash is required.", nameof(passwordHash));
    }

    public void SetRoles(IEnumerable<UserRole> roles)
    {
        UserRoles = roles.ToList();
    }

  

    public void Disable()
    {
        IsActive = false;
    }

    public void UpdateProfile(string fullName, string email, string? profileImageUrl = null)
    {
        if (!string.IsNullOrWhiteSpace(fullName))
            FullName = fullName.Trim();
        if (!string.IsNullOrWhiteSpace(email))
            Email = EmailAddress.Create(email).Value;
        if (profileImageUrl != null)
            ProfileImageUrl = profileImageUrl;
    }

    public void UpdatePassword(string passwordHash)
    {
        PasswordHash = !string.IsNullOrWhiteSpace(passwordHash)
            ? passwordHash
            : throw new ArgumentException("Password hash is required.", nameof(passwordHash));
    }
}
