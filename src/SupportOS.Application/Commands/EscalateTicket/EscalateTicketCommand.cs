using MediatR;
using SupportOS.Application.Common;

namespace SupportOS.Application.Commands.EscalateTicket;

public record EscalateTicketCommand(Guid TicketId, Guid IdempotencyKey) : IRequest<Result>, IIdempotentCommand;
