namespace ProjectNamePlaceholder.Application.Common.Interfaces.Security;

public interface IPasswordResetTokenGenerator
{
    PasswordResetTokenGenerationResult Generate();
}
