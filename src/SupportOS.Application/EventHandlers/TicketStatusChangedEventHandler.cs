using MediatR;
using Microsoft.Extensions.Logging;
using SupportOS.Domain.Events;

namespace SupportOS.Application.EventHandlers;

public class TicketStatusChangedEventHandler : INotificationHandler<TicketStatusChangedEvent>
{
    private readonly ILogger<TicketStatusChangedEventHandler> _logger;

    public TicketStatusChangedEventHandler(ILogger<TicketStatusChangedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(TicketStatusChangedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Ticket status changed: TicketId={TicketId}, {OldStatus} → {NewStatus}",
            notification.TicketId, notification.OldStatus, notification.NewStatus);
        return Task.CompletedTask;
    }
}
