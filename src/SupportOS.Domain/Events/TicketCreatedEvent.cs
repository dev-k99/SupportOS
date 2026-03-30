using MediatR;
using SupportOS.Domain.Enums;

namespace SupportOS.Domain.Events;

public record TicketCreatedEvent(Guid TicketId, Guid CustomerId, Priority Priority) : INotification;
