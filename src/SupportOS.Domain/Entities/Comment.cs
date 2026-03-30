using SupportOS.Domain.Common;

namespace SupportOS.Domain.Entities;

public class Comment : BaseEntity
{
    public Guid TicketId { get; set; }
    public Guid AuthorId { get; set; }
    public string Body { get; set; } = string.Empty;
    public bool IsInternal { get; set; }

    public Ticket Ticket { get; set; } = null!;
    public User Author { get; set; } = null!;
}
