namespace ProjectNamePlaceholder.Application.Notifications.Dtos;

public sealed record NotificationDto(
    long Id,
    string Type,
    string Message,
    bool IsRead,
    DateTime CreatedAt
);
