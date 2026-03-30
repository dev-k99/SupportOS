namespace SupportOS.Application.DTOs;

public record DashboardMetricsDto(
    int TotalTickets,
    int OpenTickets,
    int InProgressTickets,
    int ResolvedToday,
    int OverdueTickets,
    double SLABreachRate,
    double AvgResolutionHours,
    Dictionary<string, int> TicketsByPriority,
    Dictionary<string, int> TicketsByCategory,
    List<AgentMetricDto> TopAgents);
