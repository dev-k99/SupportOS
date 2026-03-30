using SupportOS.Domain.Entities;
using SupportOS.Domain.Enums;

namespace SupportOS.Domain.Interfaces;

public interface ITicketRepository
{
    Task<Ticket?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(IEnumerable<Ticket> Items, int TotalCount)> GetPagedAsync(
        TicketStatus? status,
        Priority? priority,
        Guid? customerId,
        Guid? agentId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<IEnumerable<Ticket>> GetOverdueAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Ticket ticket, CancellationToken cancellationToken = default);
    Task<List<Ticket>> GetAllAsync(CancellationToken cancellationToken = default);
}
