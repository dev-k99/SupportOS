using MediatR;
using Microsoft.Extensions.Logging;
using SupportOS.Domain.Events;

namespace SupportOS.Application.EventHandlers;

public class TicketCreatedEventHandler : INotificationHandler<TicketCreatedEvent>
{
    private readonly ILogger<TicketCreatedEventHandler> _logger;

    public TicketCreatedEventHandler(ILogger<TicketCreatedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(TicketCreatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Ticket created: TicketId={TicketId}, CustomerId={CustomerId}, Priority={Priority}",
            notification.TicketId, notification.CustomerId, notification.Priority);
        return Task.CompletedTask;
    }
}
