using MediatR;
using SupportOS.Application.Common;
using SupportOS.Application.DTOs;
using SupportOS.Domain.Enums;
using SupportOS.Domain.Interfaces;

namespace SupportOS.Application.Queries.GetTicketById;

public class GetTicketByIdQueryHandler : IRequestHandler<GetTicketByIdQuery, Result<TicketDetailDto>>
{
    private readonly ITicketRepository _ticketRepository;

    public GetTicketByIdQueryHandler(ITicketRepository ticketRepository)
    {
        _ticketRepository = ticketRepository;
    }

    public async Task<Result<TicketDetailDto>> Handle(GetTicketByIdQuery request, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId, cancellationToken);
        if (ticket is null)
            return Result<TicketDetailDto>.Failure("Ticket not found.");

        var breachMinutes = ticket.IsOverdue
            ? (int)(DateTime.UtcNow - ticket.SLADueAt).TotalMinutes
            : 0;

        var comments = ticket.Comments
            .Where(c => request.RequesterRole != UserRole.Customer || !c.IsInternal)
            .OrderBy(c => c.CreatedAt)
            .Select(c => new CommentDto(c.Id, c.Body, c.Author?.Name ?? string.Empty, c.IsInternal, c.CreatedAt))
            .ToList();

        var dto = new TicketDetailDto(
            ticket.Id,
            ticket.Title,
            ticket.Description,
            ticket.Status,
            ticket.Priority,
            ticket.Category?.Name ?? string.Empty,
            ticket.Customer?.Name ?? string.Empty,
            ticket.AssignedAgent?.Name,
            ticket.SLADueAt,
            ticket.IsOverdue,
            breachMinutes,
            ticket.CreatedAt,
            ticket.FirstResponseAt,
            ticket.FirstResponseTime,
            ticket.ResolvedAt,
            comments);

        return Result<TicketDetailDto>.Success(dto);
    }
}
