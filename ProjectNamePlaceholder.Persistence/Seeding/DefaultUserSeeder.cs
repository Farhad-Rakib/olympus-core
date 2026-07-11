using System.Security.Cryptography;
using System.Text;
using ProjectNamePlaceholder.Application.Common.Interfaces.Security;
using Microsoft.EntityFrameworkCore;
using ProjectNamePlaceholder.Domain.Entities;

namespace ProjectNamePlaceholder.Persistence.Seeding;

public static class DefaultUserSeeder
{
    public static async Task SeedAsync(DbContext dbContext, IPasswordHasher passwordHasher)
    {
        const string superAdminEmail = "superadmin@localhost";
        const string superAdminPassword = "SuperAdmin@123!";

        if (await dbContext.Set<User>().AnyAsync(u => u.Email == superAdminEmail))
        {
            return;
        }

        var superAdminRole = await dbContext.Set<Role>()
            .FirstOrDefaultAsync(role => role.Name == ProjectNamePlaceholder.Domain.Enums.SystemRoles.SuperAdmin);

        var superAdmin = new User(
            fullName: "Super Admin",
            email: superAdminEmail,
            passwordHash: passwordHasher.Hash(superAdminPassword)
        );

        if (superAdminRole != null)
        {
            var userRole = new UserRole { User = superAdmin, Role = superAdminRole };
            superAdmin.SetRoles(new[] { userRole });
        }

        dbContext.Add(superAdmin);
        await dbContext.SaveChangesAsync();
    }
}
