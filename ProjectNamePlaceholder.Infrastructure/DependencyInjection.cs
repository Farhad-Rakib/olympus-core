using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectNamePlaceholder.Application.Common.Interfaces.Security;
using ProjectNamePlaceholder.Application.Common.Interfaces.Services;
using ProjectNamePlaceholder.Infrastructure.Authentication;
using ProjectNamePlaceholder.Infrastructure.Email;
using ProjectNamePlaceholder.Infrastructure.Security;

namespace ProjectNamePlaceholder.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));

        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IRefreshTokenGenerator, RefreshTokenGenerator>();
        services.AddScoped<IPasswordResetTokenGenerator, PasswordResetTokenGenerator>();
        services.AddScoped<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddScoped<IEmailService, SmtpEmailService>();

        return services;
    }
}
