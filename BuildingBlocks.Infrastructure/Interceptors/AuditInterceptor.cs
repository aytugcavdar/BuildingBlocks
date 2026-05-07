using BuildingBlocks.Core.Audit;
using BuildingBlocks.Core.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace BuildingBlocks.Infrastructure.Interceptors;

public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly IServiceProvider _serviceProvider;

    public AuditInterceptor(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not IAuditDbContext)
            return base.SavingChangesAsync(eventData, result, cancellationToken);

        var context = eventData.Context;

        var entries = context.ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted)
            .ToList();

        if (entries.Count == 0)
            return base.SavingChangesAsync(eventData, result, cancellationToken);

        using var scope = _serviceProvider.CreateScope();
        var currentUserService = scope.ServiceProvider.GetService<ICurrentUserService>();
        var userId = currentUserService?.UserId ?? "System";

        var auditLogs = new List<AuditLog>();

        foreach (var entry in entries)
        {
            // AuditLog'un kendisini kaydetmeye çalışırsak sonsuz döngüye gireriz.
            if (entry.Entity is AuditLog)
                continue;

            var auditLog = new AuditLog
            {
                TableName = entry.Metadata.GetTableName() ?? entry.Entity.GetType().Name,
                UserId = userId,
                Action = entry.State.ToString(),
                CreatedOn = DateTime.UtcNow
            };

            var primaryKey = entry.Metadata.FindPrimaryKey();
            if (primaryKey != null)
            {
                var keys = new Dictionary<string, object>();
                foreach (var prop in primaryKey.Properties)
                {
                    keys[prop.Name] = entry.Property(prop.Name).CurrentValue!;
                }
                auditLog.KeyValues = JsonSerializer.Serialize(keys);
            }

            if (entry.State == EntityState.Added)
            {
                auditLog.NewValues = GetValues(entry, true);
            }
            else if (entry.State == EntityState.Deleted)
            {
                auditLog.OldValues = GetValues(entry, false);
            }
            else if (entry.State == EntityState.Modified)
            {
                var originalValues = new Dictionary<string, object?>();
                var currentValues = new Dictionary<string, object?>();

                foreach (var prop in entry.Properties)
                {
                    if (prop.IsModified)
                    {
                        originalValues[prop.Metadata.Name] = prop.OriginalValue;
                        currentValues[prop.Metadata.Name] = prop.CurrentValue;
                    }
                }

                auditLog.OldValues = JsonSerializer.Serialize(originalValues);
                auditLog.NewValues = JsonSerializer.Serialize(currentValues);
            }

            auditLogs.Add(auditLog);
        }

        if (auditLogs.Any())
        {
            context.Set<AuditLog>().AddRange(auditLogs);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private string GetValues(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry, bool isNew)
    {
        var values = new Dictionary<string, object?>();
        foreach (var prop in entry.Properties)
        {
            if (prop.Metadata.IsPrimaryKey()) continue;
            
            values[prop.Metadata.Name] = isNew ? prop.CurrentValue : prop.OriginalValue;
        }
        return JsonSerializer.Serialize(values);
    }
}
