using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ProjectNamePlaceholder.Api.Middleware;

public sealed class TokenValidationMiddleware
{
    private readonly RequestDelegate _next;

    public TokenValidationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint is null)
        {
            await _next(context);
            return;
        }

        var allowsAnonymous = endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null;
        var requiresAuthorization = endpoint.Metadata.GetMetadata<IAuthorizeData>() is not null;

        if (requiresAuthorization && !allowsAnonymous)
        {
            var sub = context.User.FindFirst("sub")?.Value;
            if (!context.User.Identity?.IsAuthenticated ?? true)
            {
                await WriteUnauthorizedAsync(context, "A valid access token is required.");
                return;
            }

            if (string.IsNullOrWhiteSpace(sub) || !long.TryParse(sub, out _))
            {
                await WriteUnauthorizedAsync(context, "Token payload is invalid.");
                return;
            }
        }

        await _next(context);
    }

    private static async Task WriteUnauthorizedAsync(HttpContext context, string detail)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        var problem = new ProblemDetails
        {
            Title = "Unauthorized",
            Detail = detail,
            Status = StatusCodes.Status401Unauthorized
        };

        await context.Response.WriteAsJsonAsync(problem);
    }
}
