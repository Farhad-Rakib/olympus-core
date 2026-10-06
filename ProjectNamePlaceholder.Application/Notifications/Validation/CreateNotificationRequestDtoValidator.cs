using FluentValidation;
using ProjectNamePlaceholder.Application.Notifications.Dtos;

namespace ProjectNamePlaceholder.Application.Notifications.Validation;

public sealed class CreateNotificationRequestDtoValidator : AbstractValidator<CreateNotificationRequestDto>
{
    public static readonly string[] AllowedTypes = ["info", "success", "warning", "error"];

    public CreateNotificationRequestDtoValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0);
        RuleFor(x => x.Type).NotEmpty().Must(t => AllowedTypes.Contains(t))
            .WithMessage($"Type must be one of: {string.Join(", ", AllowedTypes)}.");
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Message).NotEmpty().MaximumLength(1000);
    }
}
