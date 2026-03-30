using SupportOS.Domain.Common;
using SupportOS.Domain.Enums;

namespace SupportOS.Domain.Entities;

public class Ticket : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TicketStatus Status { get; set; } = TicketStatus.Open;
    public Priority Priority { get; set; } = Priority.Medium;
    public Guid CategoryId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid? AssignedAgentId { get; set; }
    public DateTime SLADueAt { get; set; }
    public DateTime? FirstResponseAt { get; set; }
    public DateTime? ResolvedAt { get; set; }

    public Category Category { get; set; } = null!;
    public User Customer { get; set; } = null!;
    public User? AssignedAgent { get; set; }
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();

    // ── Domain logic ────────────────────────────────────────────────────────

    /// <summary>True when the SLA deadline has passed and the ticket is still active.</summary>
    public bool IsOverdue =>
        DateTime.UtcNow > SLADueAt &&
        Status != TicketStatus.Resolved &&
        Status != TicketStatus.Closed;

    /// <summary>Elapsed time from creation to first agent response.</summary>
    public TimeSpan? FirstResponseTime =>
        FirstResponseAt.HasValue ? FirstResponseAt.Value - CreatedAt : null;

    /// <summary>
    /// Returns true when transitioning from the current status to <paramref name="newStatus"/>
    /// is a valid business operation.
    /// </summary>
    public bool CanTransitionTo(TicketStatus newStatus) =>
        (Status, newStatus) switch
        {
            // Normal workflow
            (TicketStatus.Open, TicketStatus.InProgress)          => true,
            (TicketStatus.InProgress, TicketStatus.PendingCustomer) => true,
            (TicketStatus.InProgress, TicketStatus.Resolved)       => true,
            (TicketStatus.PendingCustomer, TicketStatus.InProgress) => true,
            (TicketStatus.PendingCustomer, TicketStatus.Resolved)   => true,
            (TicketStatus.Resolved, TicketStatus.Closed)           => true,
            // Reopen
            (TicketStatus.Resolved, TicketStatus.InProgress)       => true,
            // Everything else (incl. re-entering same status) is invalid
            _ => false
        };

    /// <summary>
    /// Records the first agent/admin response if not already recorded.
    /// Should be called when an agent adds a comment or changes status from Open.
    /// </summary>
    public void RecordFirstResponse(DateTime responseAt)
    {
        if (FirstResponseAt is null)
            FirstResponseAt = responseAt;
    }
}
