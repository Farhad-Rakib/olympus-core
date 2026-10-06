namespace ProjectNamePlaceholder.Application.Notifications.Dtos;

public sealed record NotificationDto(
    long Id,
    string Type,
    string Title,
    string Message,
    bool IsRead,
    DateTime CreatedAt
);
