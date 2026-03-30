using Microsoft.EntityFrameworkCore;
using SupportOS.Domain.Entities;
using SupportOS.Domain.Enums;

namespace SupportOS.Infrastructure.Persistence;

// NOTE: User seeding (with password hashes) is intentionally NOT done via HasData.
// BCrypt generates a new random salt on every call, causing spurious EF migrations.
// User seeding is handled by DataSeeder which runs at startup and only inserts if
// users do not already exist.

public class SupportOSDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public SupportOSDbContext(DbContextOptions<SupportOSDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.HasOne(t => t.Category)
                .WithMany(c => c.Tickets)
                .HasForeignKey(t => t.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.Customer)
                .WithMany(u => u.CustomerTickets)
                .HasForeignKey(t => t.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.AssignedAgent)
                .WithMany(u => u.AssignedTickets)
                .HasForeignKey(t => t.AssignedAgentId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(t => t.Status);
            entity.HasIndex(t => t.CustomerId);
            entity.HasIndex(t => t.AssignedAgentId);
            entity.HasIndex(t => t.SLADueAt);
        });

        modelBuilder.Entity<Comment>(entity =>
        {
            entity.HasOne(c => c.Ticket)
                .WithMany(t => t.Comments)
                .HasForeignKey(c => c.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(c => c.Author)
                .WithMany(u => u.Comments)
                .HasForeignKey(c => c.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Seed data with fixed GUIDs
        // Users are seeded by DataSeeder at startup (not here) to avoid BCrypt random-salt issues.
        var customerId = new Guid("33333333-3333-3333-3333-333333333333");
        var hardwareCategoryId = new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var softwareCategoryId = new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var ticket1Id = new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var ticket2Id = new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd");
        var ticket3Id = new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        var agentId = new Guid("22222222-2222-2222-2222-222222222222");
        var seedDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<Category>().HasData(
            new Category
            {
                Id = hardwareCategoryId,
                Name = "Hardware",
                DefaultSLAHours = 24,
                CreatedAt = seedDate,
                UpdatedAt = seedDate
            },
            new Category
            {
                Id = softwareCategoryId,
                Name = "Software",
                DefaultSLAHours = 8,
                CreatedAt = seedDate,
                UpdatedAt = seedDate
            });

        modelBuilder.Entity<Ticket>().HasData(
            new Ticket
            {
                Id = ticket1Id,
                Title = "Laptop screen flickering",
                Description = "The laptop screen flickers intermittently when running on battery power.",
                Status = TicketStatus.Open,
                Priority = Priority.Medium,
                CategoryId = hardwareCategoryId,
                CustomerId = customerId,
                AssignedAgentId = null,
                SLADueAt = seedDate.AddHours(24),
                CreatedAt = seedDate,
                UpdatedAt = seedDate
            },
            new Ticket
            {
                Id = ticket2Id,
                Title = "VPN connection drops every hour",
                Description = "Corporate VPN disconnects automatically after approximately 60 minutes of use.",
                Status = TicketStatus.InProgress,
                Priority = Priority.High,
                CategoryId = softwareCategoryId,
                CustomerId = customerId,
                AssignedAgentId = agentId,
                SLADueAt = seedDate.AddHours(8),
                FirstResponseAt = seedDate.AddMinutes(30),
                CreatedAt = seedDate,
                UpdatedAt = seedDate
            },
            new Ticket
            {
                Id = ticket3Id,
                Title = "Email client not syncing",
                Description = "Outlook is not syncing new emails. Inbox shows emails from 2 days ago as the latest.",
                Status = TicketStatus.Resolved,
                Priority = Priority.Low,
                CategoryId = softwareCategoryId,
                CustomerId = customerId,
                AssignedAgentId = agentId,
                SLADueAt = seedDate.AddHours(48),
                FirstResponseAt = seedDate.AddHours(1),
                ResolvedAt = seedDate.AddHours(6),
                CreatedAt = seedDate,
                UpdatedAt = seedDate
            });
    }
}
