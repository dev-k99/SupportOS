using MediatR;
using SupportOS.Application.Common;

namespace SupportOS.Application.Commands.CloseTicket;

public record CloseTicketCommand(Guid TicketId, Guid IdempotencyKey) : IRequest<Result>, IIdempotentCommand;
