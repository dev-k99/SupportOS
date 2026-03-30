using MediatR;
using Microsoft.Extensions.Logging;
using SupportOS.Domain.Events;

namespace SupportOS.Application.EventHandlers;

public class TicketAssignedEventHandler : INotificationHandler<TicketAssignedEvent>
{
    private readonly ILogger<TicketAssignedEventHandler> _logger;

    public TicketAssignedEventHandler(ILogger<TicketAssignedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(TicketAssignedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Ticket assigned: TicketId={TicketId}, AgentId={AgentId}",
            notification.TicketId, notification.AgentId);
        return Task.CompletedTask;
    }
}
