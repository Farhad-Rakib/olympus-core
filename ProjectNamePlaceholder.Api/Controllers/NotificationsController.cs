using Asp.Versioning;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectNamePlaceholder.Api.Common;
using ProjectNamePlaceholder.Application.Notifications;
using ProjectNamePlaceholder.Application.Notifications.Dtos;
using ProjectNamePlaceholder.Application.Security;

namespace ProjectNamePlaceholder.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public sealed class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly IValidator<CreateNotificationRequestDto> _createValidator;

    public NotificationsController(INotificationService notificationService, IValidator<CreateNotificationRequestDto> createValidator)
    {
        _notificationService = notificationService;
        _createValidator = createValidator;
    }

    /// <summary>The signed-in user's most recent notifications, newest first.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<NotificationDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken)
    {
        var notifications = await _notificationService.GetNotificationsAsync(CurrentUserId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<NotificationDto>>.SuccessResponse(notifications, "Notifications retrieved successfully"));
    }

    [HttpPost("{id:long}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkAsRead(long id, CancellationToken cancellationToken)
    {
        await _notificationService.MarkAsReadAsync(CurrentUserId, id, cancellationToken);
        return NoContent();
    }

    [HttpPost("read-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken)
    {
        await _notificationService.MarkAllAsReadAsync(CurrentUserId, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        await _notificationService.DeleteNotificationAsync(CurrentUserId, id, cancellationToken);
        return NoContent();
    }

    /// <summary>Sends a notification to a user; it is pushed in real time over the notification hub.</summary>
    [HttpPost]
    [Authorize(Policy = Permissions.NotificationsCreate)]
    [ProducesResponseType(typeof(ApiResponse<NotificationDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateNotificationRequestDto request, CancellationToken cancellationToken)
    {
        await _createValidator.ValidateAndThrowAsync(request, cancellationToken);
        var created = await _notificationService.CreateNotificationAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created,
            ApiResponse<NotificationDto>.SuccessResponse(created, "Notification created", StatusCodes.Status201Created));
    }

    private long CurrentUserId =>
        long.TryParse(User.FindFirst("sub")?.Value, out var id)
            ? id
            : throw new UnauthorizedAccessException();
}
