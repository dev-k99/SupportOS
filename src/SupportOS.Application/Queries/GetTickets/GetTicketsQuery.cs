using MediatR;
using SupportOS.Application.Common;
using SupportOS.Application.DTOs;
using SupportOS.Domain.Enums;

namespace SupportOS.Application.Queries.GetTickets;

public record GetTicketsQuery(
    TicketStatus? Status,
    Priority? Priority,
    Guid RequestingUserId,
    UserRole RequesterRole,
    int PageNumber = 1,
    int PageSize = 10) : IRequest<Result<PagedResult<TicketSummaryDto>>>;
