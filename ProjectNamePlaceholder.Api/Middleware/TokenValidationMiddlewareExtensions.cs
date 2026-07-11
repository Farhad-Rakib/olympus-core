namespace ProjectNamePlaceholder.Api.Middleware;

public static class TokenValidationMiddlewareExtensions
{
    public static IApplicationBuilder UseTokenValidation(this IApplicationBuilder app)
    {
        return app.UseMiddleware<TokenValidationMiddleware>();
    }
}
