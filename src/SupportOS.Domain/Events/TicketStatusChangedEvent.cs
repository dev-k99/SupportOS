using MediatR;
using SupportOS.Domain.Enums;

namespace SupportOS.Domain.Events;

public record TicketStatusChangedEvent(Guid TicketId, TicketStatus OldStatus, TicketStatus NewStatus) : INotification;
