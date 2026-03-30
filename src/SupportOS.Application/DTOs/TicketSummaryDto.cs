using SupportOS.Domain.Enums;

namespace SupportOS.Application.DTOs;

public record TicketSummaryDto(
    Guid Id,
    string Title,
    TicketStatus Status,
    Priority Priority,
    string CategoryName,
    string CustomerName,
    string? AgentName,
    DateTime SLADueAt,
    bool IsOverdue,
    DateTime CreatedAt);
