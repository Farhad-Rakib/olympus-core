using FluentValidation;
using ProjectNamePlaceholder.Application.Auth.Dtos;

namespace ProjectNamePlaceholder.Application.Auth.Validation;

public sealed class ForgotPasswordRequestDtoValidator : AbstractValidator<ForgotPasswordRequestDto>
{
    public ForgotPasswordRequestDtoValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}
