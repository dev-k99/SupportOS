using MediatR;
using SupportOS.Application.Common;
using SupportOS.Application.DTOs;
using SupportOS.Domain.Enums;
using SupportOS.Domain.Interfaces;

namespace SupportOS.Application.Queries.GetTickets;

public class GetTicketsQueryHandler : IRequestHandler<GetTicketsQuery, Result<PagedResult<TicketSummaryDto>>>
{
    private readonly ITicketRepository _ticketRepository;

    public GetTicketsQueryHandler(ITicketRepository ticketRepository)
    {
        _ticketRepository = ticketRepository;
    }

    public async Task<Result<PagedResult<TicketSummaryDto>>> Handle(GetTicketsQuery request, CancellationToken cancellationToken)
    {
        Guid? customerId = null;
        Guid? agentId = null;

        if (request.RequesterRole == UserRole.Customer)
            customerId = request.RequestingUserId;
        else if (request.RequesterRole == UserRole.Agent)
            agentId = request.RequestingUserId;

        var pageSize = Math.Min(request.PageSize, 50);

        var (items, total) = await _ticketRepository.GetPagedAsync(
            request.Status,
            request.Priority,
            customerId,
            agentId,
            request.PageNumber,
            pageSize,
            cancellationToken);

        var now = DateTime.UtcNow;
        var dtos = items.Select(t => new TicketSummaryDto(
            t.Id,
            t.Title,
            t.Status,
            t.Priority,
            t.Category?.Name ?? string.Empty,
            t.Customer?.Name ?? string.Empty,
            t.AssignedAgent?.Name,
            t.SLADueAt,
            now > t.SLADueAt && t.Status != TicketStatus.Resolved && t.Status != TicketStatus.Closed,
            t.CreatedAt)).ToList();

        return Result<PagedResult<TicketSummaryDto>>.Success(
            new PagedResult<TicketSummaryDto>(dtos, total, request.PageNumber, pageSize));
    }
}
