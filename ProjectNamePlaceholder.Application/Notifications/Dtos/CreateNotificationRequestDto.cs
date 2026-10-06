namespace ProjectNamePlaceholder.Application.Notifications.Dtos;

public sealed record CreateNotificationRequestDto(
    long UserId,
    string Type,
    string Title,
    string Message
);
