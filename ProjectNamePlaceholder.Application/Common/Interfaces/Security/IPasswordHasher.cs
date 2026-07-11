namespace ProjectNamePlaceholder.Application.Common.Interfaces.Security;

public interface IPasswordHasher
{
    string Hash(string plainText);
    bool Verify(string plainText, string hash);
}
