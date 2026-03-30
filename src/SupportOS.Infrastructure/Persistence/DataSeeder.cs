using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SupportOS.Domain.Entities;
using SupportOS.Domain.Enums;
using SupportOS.Domain.Services;

namespace SupportOS.Infrastructure.Persistence;

/// <summary>
/// Seeds initial data at startup. Each section is idempotent — it only inserts
/// when the target table is empty, so this is safe to call on every application start.
///
/// Users are seeded here (not via HasData) because BCrypt generates a new random
/// salt on every call, which would cause spurious EF migrations.
/// Tickets are seeded here (not via HasData) because they FK-reference users who
/// only exist after this seeder runs — i.e., after the migration completes.
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
        await SeedUsersAsync(cancellationToken);
        await SeedTicketsAsync(cancellationToken);
    }

    private async Task SeedUsersAsync(CancellationToken cancellationToken)
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
                Id           = new Guid("11111111-1111-1111-1111-111111111111"),
                Name         = "Admin User",
                Email        = "admin@supportos.io",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@1234", workFactor: 12),
                Role         = UserRole.Admin,
                CreatedAt    = seedDate,
                UpdatedAt    = seedDate
            },
            new User
            {
                Id           = new Guid("22222222-2222-2222-2222-222222222222"),
                Name         = "Support Agent",
                Email        = "agent@supportos.io",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Agent@1234", workFactor: 12),
                Role         = UserRole.Agent,
                CreatedAt    = seedDate,
                UpdatedAt    = seedDate
            },
            new User
            {
                Id           = new Guid("33333333-3333-3333-3333-333333333333"),
                Name         = "Demo Customer",
                Email        = "customer@supportos.io",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Customer@1234", workFactor: 12),
                Role         = UserRole.Customer,
                CreatedAt    = seedDate,
                UpdatedAt    = seedDate
            });

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("DataSeeder: seeded 3 users (admin, agent, customer).");
    }

    private async Task SeedTicketsAsync(CancellationToken cancellationToken)
    {
        if (await _db.Tickets.AnyAsync(cancellationToken))
        {
            _logger.LogDebug("DataSeeder: tickets already present, skipping.");
            return;
        }

        _logger.LogInformation("DataSeeder: seeding demo tickets.");

        var hardwareCategoryId = new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var softwareCategoryId = new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var agentId            = new Guid("22222222-2222-2222-2222-222222222222");
        var customerId         = new Guid("33333333-3333-3333-3333-333333333333");
        var seedDate           = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        _db.Tickets.AddRange(
            new Ticket
            {
                Id          = new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                Title       = "Laptop screen flickering",
                Description = "The laptop screen flickers intermittently when running on battery power.",
                Status      = TicketStatus.Open,
                Priority    = Priority.Medium,
                CategoryId  = hardwareCategoryId,
                CustomerId  = customerId,
                SLADueAt    = SLACalculator.CalculateDueDate(Priority.Medium, seedDate),
                CreatedAt   = seedDate,
                UpdatedAt   = seedDate
            },
            new Ticket
            {
                Id              = new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd"),
                Title           = "VPN connection drops every hour",
                Description     = "Corporate VPN disconnects automatically after approximately 60 minutes of use.",
                Status          = TicketStatus.InProgress,
                Priority        = Priority.High,
                CategoryId      = softwareCategoryId,
                CustomerId      = customerId,
                AssignedAgentId = agentId,
                FirstResponseAt = seedDate.AddMinutes(30),
                SLADueAt        = SLACalculator.CalculateDueDate(Priority.High, seedDate),
                CreatedAt       = seedDate,
                UpdatedAt       = seedDate
            },
            new Ticket
            {
                Id              = new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
                Title           = "Email client not syncing",
                Description     = "Outlook is not syncing new emails. Inbox shows emails from 2 days ago as the latest.",
                Status          = TicketStatus.Resolved,
                Priority        = Priority.Low,
                CategoryId      = softwareCategoryId,
                CustomerId      = customerId,
                AssignedAgentId = agentId,
                FirstResponseAt = seedDate.AddHours(1),
                ResolvedAt      = seedDate.AddHours(6),
                SLADueAt        = SLACalculator.CalculateDueDate(Priority.Low, seedDate),
                CreatedAt       = seedDate,
                UpdatedAt       = seedDate
            });

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("DataSeeder: seeded 3 demo tickets.");
    }
}
