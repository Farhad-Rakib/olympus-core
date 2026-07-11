using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using ProjectNamePlaceholder.Domain.Enums;

namespace ProjectNamePlaceholder.Api.Authorization;

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {

        // SuperAdmin shortcut: users with the SuperAdmin role bypass permission checks
        if (context.User.IsInRole(SystemRoles.SuperAdmin))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        var hasPermission = context.User.Claims
            .Where(c => c.Type == "permission")
            .Any(c => string.Equals(c.Value, requirement.Permission, StringComparison.OrdinalIgnoreCase));

        if (hasPermission)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
