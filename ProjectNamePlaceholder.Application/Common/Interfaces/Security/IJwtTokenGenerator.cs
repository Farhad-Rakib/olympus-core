using ProjectNamePlaceholder.Domain.Entities;

namespace ProjectNamePlaceholder.Application.Common.Interfaces.Security;

public interface IJwtTokenGenerator
{
    string GenerateAccessToken(User user, IEnumerable<string> permissions);
    DateTime GetAccessTokenExpiryUtc();
}
