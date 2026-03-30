using MediatR;
using SupportOS.Application.Common;

namespace SupportOS.Application.Commands.AssignTicket;

public record AssignTicketCommand(Guid TicketId, Guid AgentId, Guid IdempotencyKey) : IRequest<Result>, IIdempotentCommand;
