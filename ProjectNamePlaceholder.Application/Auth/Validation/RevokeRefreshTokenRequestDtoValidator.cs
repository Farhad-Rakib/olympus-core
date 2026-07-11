using FluentValidation;
using ProjectNamePlaceholder.Application.Auth.Dtos;

namespace ProjectNamePlaceholder.Application.Auth.Validation;

public sealed class RevokeRefreshTokenRequestDtoValidator : AbstractValidator<RevokeRefreshTokenRequestDto>
{
    public RevokeRefreshTokenRequestDtoValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}
