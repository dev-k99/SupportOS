using MediatR;
using SupportOS.Application.Common;
using SupportOS.Application.DTOs;
using SupportOS.Domain.Enums;
using SupportOS.Domain.Interfaces;

namespace SupportOS.Application.Queries.GetOverdueTickets;

public class GetOverdueTicketsQueryHandler : IRequestHandler<GetOverdueTicketsQuery, Result<List<TicketSummaryDto>>>
{
    private readonly ITicketRepository _ticketRepository;

    public GetOverdueTicketsQueryHandler(ITicketRepository ticketRepository)
    {
        _ticketRepository = ticketRepository;
    }

    public async Task<Result<List<TicketSummaryDto>>> Handle(GetOverdueTicketsQuery request, CancellationToken cancellationToken)
    {
        var tickets = await _ticketRepository.GetOverdueAsync(cancellationToken);
        var now = DateTime.UtcNow;

        var dtos = tickets
            .Where(t => t.Status != TicketStatus.Resolved && t.Status != TicketStatus.Closed)
            .Select(t => new TicketSummaryDto(
                t.Id,
                t.Title,
                t.Status,
                t.Priority,
                t.Category?.Name ?? string.Empty,
                t.Customer?.Name ?? string.Empty,
                t.AssignedAgent?.Name,
                t.SLADueAt,
                true,
                t.CreatedAt))
            .ToList();

        return Result<List<TicketSummaryDto>>.Success(dtos);
    }
}
