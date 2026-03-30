using SupportOS.Domain.Common;

namespace SupportOS.Domain.Entities;

public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public int DefaultSLAHours { get; set; }

    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
