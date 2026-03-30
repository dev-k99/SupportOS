using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SupportOS.Domain.Entities;

namespace SupportOS.Infrastructure.Persistence;

public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditInterceptor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
            await AuditChangesAsync(eventData.Context, cancellationToken);

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private async Task AuditChangesAsync(DbContext context, CancellationToken cancellationToken)
    {
        var changedBy = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Email)?.Value ?? "System";
        var now = DateTime.UtcNow;

        var entries = context.ChangeTracker.Entries()
            .Where(e => e.Entity is not AuditLog
                     && e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        foreach (var entry in entries)
        {
            var entityName = entry.Entity.GetType().Name;
            var entityId = entry.Properties
                .FirstOrDefault(p => p.Metadata.Name == "Id")?.CurrentValue?.ToString() ?? string.Empty;

            string action;
            string? before = null;
            string after;

            var serializerOptions = new JsonSerializerOptions { WriteIndented = false };

            if (entry.State == EntityState.Added)
            {
                action = "Created";
                after = JsonSerializer.Serialize(
                    entry.Properties.ToDictionary(p => p.Metadata.Name, p => p.CurrentValue),
                    serializerOptions);
            }
            else if (entry.State == EntityState.Modified)
            {
                action = "Updated";
                before = JsonSerializer.Serialize(
                    entry.Properties.ToDictionary(p => p.Metadata.Name, p => p.OriginalValue),
                    serializerOptions);
                after = JsonSerializer.Serialize(
                    entry.Properties.ToDictionary(p => p.Metadata.Name, p => p.CurrentValue),
                    serializerOptions);
            }
            else
            {
                action = "Deleted";
                before = JsonSerializer.Serialize(
                    entry.Properties.ToDictionary(p => p.Metadata.Name, p => p.OriginalValue),
                    serializerOptions);
                after = JsonSerializer.Serialize(new { Deleted = true }, serializerOptions);
            }

            var audit = new AuditLog
            {
                EntityName = entityName,
                EntityId = entityId,
                Action = action,
                Before = before,
                After = after,
                ChangedBy = changedBy,
                ChangedAt = now,
                CreatedAt = now,
                UpdatedAt = now
            };

            await context.Set<AuditLog>().AddAsync(audit, cancellationToken);
        }
    }
}
