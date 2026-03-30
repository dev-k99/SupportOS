using MediatR;
using SupportOS.Application.Common;
using SupportOS.Application.DTOs;
using SupportOS.Domain.Enums;

namespace SupportOS.Application.Commands.CreateTicket;

public record CreateTicketCommand(
    string Title,
    string Description,
    Priority Priority,
    Guid CategoryId,
    Guid CustomerId,
    Guid IdempotencyKey) : IRequest<Result<TicketSummaryDto>>, IIdempotentCommand;
