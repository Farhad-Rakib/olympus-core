using FluentValidation;
using ProjectNamePlaceholder.Application.Auth.Dtos;

namespace ProjectNamePlaceholder.Application.Auth.Validation;

public sealed class LoginRequestDtoValidator : AbstractValidator<LoginRequestDto>
{
    public LoginRequestDtoValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
    }
}
