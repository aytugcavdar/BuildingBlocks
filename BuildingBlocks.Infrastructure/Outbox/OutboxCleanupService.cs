using BuildingBlocks.Core.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Infrastructure.Outbox;

/// <summary>
/// Eski outbox mesajlarını temizleyen background service
/// Her gün çalışır ve belirli süre önceki işlenmiş mesajları siler
/// </summary>
/// <typeparam name="TDbContext">DbContext tipi</typeparam>
public class OutboxCleanupService<TDbContext> : BackgroundService
    where TDbContext : DbContext
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxCleanupService<TDbContext>> _logger;
    private readonly TimeSpan _interval;
    private readonly int _retentionDays;

    public OutboxCleanupService(
        IServiceProvider serviceProvider,
        ILogger<OutboxCleanupService<TDbContext>> logger,
        TimeSpan? interval = null,
        int retentionDays = 7)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _interval = interval ?? TimeSpan.FromDays(1);
        _retentionDays = retentionDays;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "🧹 OutboxCleanupService started - Interval: {Interval} days, Retention: {Retention} days",
            _interval.TotalDays,
            _retentionDays);

        // İlk çalıştırmayı 1 saat sonra yap
        await Task.Delay(TimeSpan.FromHours(1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupOldMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in OutboxCleanupService");
            }

            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("🧹 OutboxCleanupService stopped");
    }

    private async Task CleanupOldMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();

        var cutoffDate = DateTime.UtcNow.AddDays(-_retentionDays);

        // İşlenmiş ve retention süresinden eski mesajları bul
        var oldMessages = await dbContext.Set<OutboxMessage>()
            .Where(m => m.ProcessedOn != null && m.ProcessedOn < cutoffDate)
            .ToListAsync(cancellationToken);

        if (!oldMessages.Any())
        {
            _logger.LogDebug("🧹 No old messages to clean up");
            return;
        }

        dbContext.Set<OutboxMessage>().RemoveRange(oldMessages);
        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "🧹 Cleaned up {Count} old outbox messages (older than {CutoffDate:yyyy-MM-dd})",
            oldMessages.Count,
            cutoffDate);
    }
}
