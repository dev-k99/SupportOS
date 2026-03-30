using MediatR;

namespace SupportOS.Domain.Events;

public record TicketAssignedEvent(Guid TicketId, Guid AgentId) : INotification;
