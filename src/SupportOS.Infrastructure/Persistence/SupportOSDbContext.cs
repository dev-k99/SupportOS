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

        // Category seed data — no FK dependency on Users, safe in migration.
        // Users and Tickets are seeded by DataSeeder at startup (after migration)
        // because Tickets reference User FKs that only exist post-seeding.
        var hardwareCategoryId = new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var softwareCategoryId = new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
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
    }
}
