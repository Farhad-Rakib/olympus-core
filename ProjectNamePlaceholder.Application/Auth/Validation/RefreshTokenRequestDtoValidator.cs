using FluentValidation;
using ProjectNamePlaceholder.Application.Auth.Dtos;

namespace ProjectNamePlaceholder.Application.Auth.Validation;

public sealed class RefreshTokenRequestDtoValidator : AbstractValidator<RefreshTokenRequestDto>
{
    public RefreshTokenRequestDtoValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}
