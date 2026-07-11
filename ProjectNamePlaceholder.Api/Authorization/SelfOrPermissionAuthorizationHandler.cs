using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using ProjectNamePlaceholder.Domain.Enums;

namespace ProjectNamePlaceholder.Api.Authorization;

public sealed class SelfOrPermissionAuthorizationHandler : AuthorizationHandler<SelfOrPermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, SelfOrPermissionRequirement requirement)
    {
        // SuperAdmin shortcut: users in SuperAdmin role bypass permission/self checks
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
            return Task.CompletedTask;
        }

        var callerId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(callerId) || !Guid.TryParse(callerId, out _))
        {
            return Task.CompletedTask;
        }

        var routeValue = ResolveRouteValue(context.Resource, requirement.RouteParameterName);
        if (!string.IsNullOrWhiteSpace(routeValue)
            && string.Equals(routeValue, callerId, StringComparison.OrdinalIgnoreCase))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }

    private static string? ResolveRouteValue(object? resource, string routeParameterName)
    {
        if (resource is HttpContext httpContext
            && httpContext.Request.RouteValues.TryGetValue(routeParameterName, out var routeValue))
        {
            return routeValue?.ToString();
        }

        if (resource is AuthorizationFilterContext filterContext
            && filterContext.RouteData.Values.TryGetValue(routeParameterName, out var mvcRouteValue))
        {
            return mvcRouteValue?.ToString();
        }

        return null;
    }
}
