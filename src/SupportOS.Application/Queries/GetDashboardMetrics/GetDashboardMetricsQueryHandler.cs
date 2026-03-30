using MediatR;
using SupportOS.Application.Common;
using SupportOS.Application.DTOs;
using SupportOS.Domain.Enums;
using SupportOS.Domain.Interfaces;

namespace SupportOS.Application.Queries.GetDashboardMetrics;

public class GetDashboardMetricsQueryHandler : IRequestHandler<GetDashboardMetricsQuery, Result<DashboardMetricsDto>>
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IUserRepository _userRepository;

    public GetDashboardMetricsQueryHandler(ITicketRepository ticketRepository, IUserRepository userRepository)
    {
        _ticketRepository = ticketRepository;
        _userRepository = userRepository;
    }

    public async Task<Result<DashboardMetricsDto>> Handle(GetDashboardMetricsQuery request, CancellationToken cancellationToken)
    {
        var allTickets = await _ticketRepository.GetAllAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var todayStart = now.Date;

        var total = allTickets.Count;
        var openCount = allTickets.Count(t => t.Status == TicketStatus.Open);
        var inProgressCount = allTickets.Count(t => t.Status == TicketStatus.InProgress);
        var resolvedToday = allTickets.Count(t => t.ResolvedAt >= todayStart && t.ResolvedAt < todayStart.AddDays(1));

        var activeTickets = allTickets.Where(t => t.Status != TicketStatus.Resolved && t.Status != TicketStatus.Closed).ToList();
        var overdueCount = activeTickets.Count(t => now > t.SLADueAt);
        var slaBreachRate = activeTickets.Count > 0
            ? Math.Round((double)overdueCount / activeTickets.Count * 100, 2)
            : 0.0;

        var resolvedTickets = allTickets.Where(t => t.ResolvedAt.HasValue).ToList();
        var avgResolutionHours = resolvedTickets.Count > 0
            ? Math.Round(resolvedTickets.Average(t => (t.ResolvedAt!.Value - t.CreatedAt).TotalHours), 2)
            : 0.0;

        var byPriority = Enum.GetValues<Priority>()
            .ToDictionary(p => p.ToString(), p => allTickets.Count(t => t.Priority == p));

        var byCategory = allTickets
            .GroupBy(t => t.Category?.Name ?? "Unknown")
            .ToDictionary(g => g.Key, g => g.Count());

        var agents = await _userRepository.GetAgentsAsync(cancellationToken);
        var topAgents = agents.Select(a => new AgentMetricDto(
            a.Name,
            allTickets.Count(t => t.AssignedAgentId == a.Id),
            allTickets.Count(t => t.AssignedAgentId == a.Id && t.Status == TicketStatus.Resolved)))
            .OrderByDescending(a => a.ResolvedCount)
            .Take(10)
            .ToList();

        return Result<DashboardMetricsDto>.Success(new DashboardMetricsDto(
            total,
            openCount,
            inProgressCount,
            resolvedToday,
            overdueCount,
            slaBreachRate,
            avgResolutionHours,
            byPriority,
            byCategory,
            topAgents));
    }
}
