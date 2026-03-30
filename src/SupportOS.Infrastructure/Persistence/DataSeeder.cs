using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SupportOS.Domain.Entities;
using SupportOS.Domain.Enums;

namespace SupportOS.Infrastructure.Persistence;

/// <summary>
/// Seeds initial users at startup. Runs only when the users table is empty,
/// so it is idempotent and safe to call on every application start.
///
/// Password hashes are computed at runtime (not baked into EF migrations) to
/// avoid the BCrypt random-salt issue where each call to HashPassword produces a
/// different value, which would trigger spurious EF migrations.
/// </summary>
public sealed class DataSeeder
{
    private readonly SupportOSDbContext _db;
    private readonly ILogger<DataSeeder> _logger;

    public DataSeeder(SupportOSDbContext db, ILogger<DataSeeder> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _db.Users.AnyAsync(cancellationToken))
        {
            _logger.LogDebug("DataSeeder: users already present, skipping.");
            return;
        }

        _logger.LogInformation("DataSeeder: seeding initial users.");

        var seedDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        _db.Users.AddRange(
            new User
            {
                Id = new Guid("11111111-1111-1111-1111-111111111111"),
                Name = "Admin User",
                Email = "admin@supportos.io",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@1234", workFactor: 12),
                Role = UserRole.Admin,
                CreatedAt = seedDate,
                UpdatedAt = seedDate
            },
            new User
            {
                Id = new Guid("22222222-2222-2222-2222-222222222222"),
                Name = "Support Agent",
                Email = "agent@supportos.io",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Agent@1234", workFactor: 12),
                Role = UserRole.Agent,
                CreatedAt = seedDate,
                UpdatedAt = seedDate
            },
            new User
            {
                Id = new Guid("33333333-3333-3333-3333-333333333333"),
                Name = "Demo Customer",
                Email = "customer@supportos.io",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Customer@1234", workFactor: 12),
                Role = UserRole.Customer,
                CreatedAt = seedDate,
                UpdatedAt = seedDate
            });

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("DataSeeder: seeded 3 users (admin, agent, customer).");
    }
}
