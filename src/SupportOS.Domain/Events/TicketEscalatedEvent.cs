using MediatR;
using SupportOS.Domain.Enums;

namespace SupportOS.Domain.Events;

public record TicketEscalatedEvent(Guid TicketId, Priority OldPriority, Priority NewPriority) : INotification;
