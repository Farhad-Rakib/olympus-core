using System.Reflection;
using ProjectNamePlaceholder.Domain.Entities;

namespace ProjectNamePlaceholder.Persistence.Dapper;

internal static class DomainEntityHydrator
{
    public static User CreateUser(UserRow row)
    {
        var user = new User(row.FullName, row.Email, row.PasswordHash);
        SetId(user, row.Id);
        SetProperty(user, nameof(User.IsActive), row.IsActive);
        return user;
    }

    public static Role CreateRole(RoleRow row)
    {
        var role = new Role(row.Name, row.Description);
        SetId(role, row.Id);
        return role;
    }

    public static Permission CreatePermission(PermissionRow row)
    {
        var permission = new Permission(row.Name, row.Description);
        SetId(permission, row.Id);
        return permission;
    }

    public static RefreshToken CreateRefreshToken(RefreshTokenRow row)
    {
        var refreshToken = new RefreshToken(row.UserId, row.TokenHash, row.ExpiresAtUtc);
        SetId(refreshToken, row.Id);
        SetProperty(refreshToken, nameof(RefreshToken.CreatedAt), row.CreatedAtUtc);
        SetProperty(refreshToken, nameof(RefreshToken.RevokedAtUtc), row.RevokedAtUtc);
        SetProperty(refreshToken, nameof(RefreshToken.ReplacedByTokenHash), row.ReplacedByTokenHash);
        return refreshToken;
    }

    private static void SetId(BaseEntity entity, long id)
    {
        SetProperty(entity, nameof(BaseEntity.Id), id);
    }

    private static void SetProperty(object target, string propertyName, object? value)
    {
        var currentType = target.GetType();
        PropertyInfo? property = null;

        while (currentType is not null && property is null)
        {
            property = currentType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            currentType = currentType.BaseType;
        }

        if (property is null)
        {
            throw new InvalidOperationException($"Property '{propertyName}' not found on '{target.GetType().Name}'.");
        }

        property.SetValue(target, value);
    }

    internal sealed record UserRow(long Id, string FullName, string Email, string PasswordHash, bool IsActive, DateTime CreatedAtUtc, DateTime? UpdatedAtUtc);
    internal sealed record RoleRow(long Id, string Name, string Description);
    internal sealed record PermissionRow(long Id, string Name, string Description);
    internal sealed record RefreshTokenRow(long Id, long UserId, string TokenHash, DateTime ExpiresAtUtc, DateTime CreatedAtUtc, DateTime? RevokedAtUtc, string? ReplacedByTokenHash);
}
