using MediatR;
using Microsoft.Extensions.Logging;
using SupportOS.Domain.Events;

namespace SupportOS.Application.EventHandlers;

public class TicketEscalatedEventHandler : INotificationHandler<TicketEscalatedEvent>
{
    private readonly ILogger<TicketEscalatedEventHandler> _logger;

    public TicketEscalatedEventHandler(ILogger<TicketEscalatedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(TicketEscalatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Ticket escalated: TicketId={TicketId}, {OldPriority} → {NewPriority}",
            notification.TicketId, notification.OldPriority, notification.NewPriority);
        return Task.CompletedTask;
    }
}
