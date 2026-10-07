using ProjectNamePlaceholder.Persistence;

namespace ProjectNamePlaceholder.Api.Startup;

/// <summary>
/// Fails startup outside Development when a deployment secret was not supplied.
/// Staging/Production appsettings leave these empty on purpose; the real values come
/// from environment variables (e.g. Jwt__SecretKey), so a missed variable stops the app
/// instead of letting it run with a blank or development value.
/// </summary>
public static class RequiredSettingsValidator
{
    private const int MinJwtSecretLength = 32;

    public static void Validate(IConfiguration configuration)
    {
        var connectionStringName = DependencyInjection.GetConnectionStringName(configuration);
        var errors = new List<string>();

        Require(configuration, $"ConnectionStrings:{connectionStringName}", errors);
        Require(configuration, "Smtp:Host", errors);
        Require(configuration, "Smtp:FromAddress", errors);

        var jwtSecret = configuration["Jwt:SecretKey"];
        if (Require(configuration, "Jwt:SecretKey", errors) && jwtSecret!.Length < MinJwtSecretLength)
        {
            errors.Add($"Jwt:SecretKey must be at least {MinJwtSecretLength} characters.");
        }

        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                "Missing required configuration. Set these as environment variables (use '__' for ':'):"
                + Environment.NewLine + string.Join(Environment.NewLine, errors.Select(e => "  - " + e)));
        }
    }

    private static bool Require(IConfiguration configuration, string key, List<string> errors)
    {
        var value = configuration[key];
        // "${...}" means a placeholder was copied in but never substituted.
        if (string.IsNullOrWhiteSpace(value) || (value.StartsWith("${") && value.EndsWith('}')))
        {
            errors.Add(key);
            return false;
        }

        return true;
    }
}
