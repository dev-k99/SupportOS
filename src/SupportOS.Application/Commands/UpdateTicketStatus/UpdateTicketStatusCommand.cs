using MediatR;
using SupportOS.Application.Common;
using SupportOS.Domain.Enums;

namespace SupportOS.Application.Commands.UpdateTicketStatus;

public record UpdateTicketStatusCommand(Guid TicketId, TicketStatus NewStatus, Guid RequestingUserId, Guid IdempotencyKey) : IRequest<Result>, IIdempotentCommand;
