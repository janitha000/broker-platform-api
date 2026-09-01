using Microsoft.AspNetCore.Mvc;
using Notification.Application.Notifications.SendNotification;
using Notification.Domain.Notifications;

namespace Notification.Api.Notifications;

[ApiController]
[Route("notifications")]
public sealed class NotificationsController : ControllerBase
{
    private readonly SendNotificationHandler _sendNotificationHandler;
    private readonly INotificationRepository _notifications;

    public NotificationsController(
        SendNotificationHandler sendNotificationHandler,
        INotificationRepository notifications)
    {
        _sendNotificationHandler = sendNotificationHandler;
        _notifications = notifications;
    }

    [HttpPost]
    public async Task<IActionResult> Send(
        [FromBody] SendNotificationCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.IdempotencyKey)
            || string.IsNullOrWhiteSpace(command.Channel)
            || string.IsNullOrWhiteSpace(command.Recipient)
            || string.IsNullOrWhiteSpace(command.Subject)
            || string.IsNullOrWhiteSpace(command.Source)
            || command.Body is null)
            return BadRequest();

        var outcome = await _sendNotificationHandler.Handle(command, cancellationToken);
        return outcome.Kind switch
        {
            SendNotificationKind.Sent => Ok(outcome.Notification),
            SendNotificationKind.Failed => StatusCode(StatusCodes.Status502BadGateway, outcome.Notification),
            SendNotificationKind.IdempotencyConflict => Conflict(),
            _ => StatusCode(StatusCodes.Status500InternalServerError),
        };
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken = default)
    {
        var notification = await _notifications.GetById(id, cancellationToken);
        if (notification is null)
            return NotFound();

        return Ok(new SendNotificationResult(notification.Id, notification.Status));
    }
}
