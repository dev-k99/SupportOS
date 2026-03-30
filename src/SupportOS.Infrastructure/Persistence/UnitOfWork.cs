using SupportOS.Domain.Interfaces;

namespace SupportOS.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly SupportOSDbContext _context;

    public UnitOfWork(SupportOSDbContext context)
    {
        _context = context;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}
