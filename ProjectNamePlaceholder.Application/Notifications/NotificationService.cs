using ProjectNamePlaceholder.Application.Common.Exceptions;
using ProjectNamePlaceholder.Application.Common.Interfaces;
using ProjectNamePlaceholder.Application.Notifications.Dtos;
using ProjectNamePlaceholder.Domain.Entities;

namespace ProjectNamePlaceholder.Application.Notifications;

public sealed class NotificationService : INotificationService
{
    private const int MaxNotificationsReturned = 50;

    private readonly INotificationRepository _notificationRepository;
    private readonly IUserRepository _userRepository;
    private readonly INotificationPublisher _publisher;
    private readonly IUnitOfWork _unitOfWork;

    public NotificationService(
        INotificationRepository notificationRepository,
        IUserRepository userRepository,
        INotificationPublisher publisher,
        IUnitOfWork unitOfWork)
    {
        _notificationRepository = notificationRepository;
        _userRepository = userRepository;
        _publisher = publisher;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<NotificationDto>> GetNotificationsAsync(long userId, CancellationToken cancellationToken = default)
    {
        var notifications = await _notificationRepository.GetForUserAsync(userId, MaxNotificationsReturned, cancellationToken);
        return notifications.Select(ToDto).ToList();
    }

    public async Task MarkAsReadAsync(long userId, long notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await GetOwnedAsync(userId, notificationId, cancellationToken);
        if (notification.IsRead) return;

        notification.MarkAsRead();
        _notificationRepository.Update(notification);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkAllAsReadAsync(long userId, CancellationToken cancellationToken = default)
    {
        var unread = await _notificationRepository.GetUnreadForUserAsync(userId, cancellationToken);
        if (unread.Count == 0) return;

        foreach (var notification in unread)
        {
            notification.MarkAsRead();
            _notificationRepository.Update(notification);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<NotificationDto> CreateNotificationAsync(CreateNotificationRequestDto request, CancellationToken cancellationToken = default)
    {
        if (await _userRepository.GetByIdAsync(request.UserId, cancellationToken) is null)
        {
            throw new NotFoundException("User not found.");
        }

        var notification = new Notification(request.UserId, request.Type, request.Title, request.Message);
        await _notificationRepository.AddAsync(notification, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = ToDto(notification);
        await _publisher.PublishAsync(request.UserId, dto, cancellationToken);
        return dto;
    }

    public async Task DeleteNotificationAsync(long userId, long notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await GetOwnedAsync(userId, notificationId, cancellationToken);
        _notificationRepository.Delete(notification);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    // Looks up by owner as well as id, so users can never touch someone else's notifications.
    private async Task<Notification> GetOwnedAsync(long userId, long notificationId, CancellationToken cancellationToken)
    {
        return await _notificationRepository.GetForUserByIdAsync(userId, notificationId, cancellationToken)
            ?? throw new NotFoundException("Notification not found.");
    }

    private static NotificationDto ToDto(Notification n) =>
        new(n.Id, n.Type, n.Title, n.Message, n.IsRead, n.CreatedAt);
}
