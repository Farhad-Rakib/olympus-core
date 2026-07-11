namespace ProjectNamePlaceholder.Domain.Entities;

public sealed class Role : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;

    public ICollection<UserRole> UserRoles { get; private set; } = new List<UserRole>();
    public ICollection<RolePermission> RolePermissions { get; private set; } = new List<RolePermission>();

    private Role()
    {
    }

    public Role(string name, string description)
    {
        Name = !string.IsNullOrWhiteSpace(name)
            ? name.Trim()
            : throw new ArgumentException("Role name is required.", nameof(name));

        Description = description?.Trim() ?? string.Empty;
    }

    public void Update(string name, string description)
    {
        Name = !string.IsNullOrWhiteSpace(name)
            ? name.Trim()
            : throw new ArgumentException("Role name is required.", nameof(name));

        Description = description?.Trim() ?? string.Empty;
    }
}
