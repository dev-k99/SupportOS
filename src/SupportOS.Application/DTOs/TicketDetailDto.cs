using SupportOS.Domain.Enums;

namespace SupportOS.Application.DTOs;

public record TicketDetailDto(
    Guid Id,
    string Title,
    string Description,
    TicketStatus Status,
    Priority Priority,
    string CategoryName,
    string CustomerName,
    string? AgentName,
    DateTime SLADueAt,
    bool IsOverdue,
    int SLABreachMinutes,
    DateTime CreatedAt,
    DateTime? FirstResponseAt,
    TimeSpan? FirstResponseTime,
    DateTime? ResolvedAt,
    IEnumerable<CommentDto> Comments);
