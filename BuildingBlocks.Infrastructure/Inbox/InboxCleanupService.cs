using BuildingBlocks.Core.Inbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Infrastructure.Inbox;

public class InboxCleanupOptions
{
    /// <summary>
    /// Servisin veritabanını tarama sıklığı (Gün). Varsayılan 1 gündür.
    /// </summary>
    public int CleanupIntervalDays { get; set; } = 1;

    /// <summary>
    /// Kaç günden eski mesajların veritabanından kalıcı silineceğini belirler.
    /// Varsayılan 7 gündür.
    /// </summary>
    public int RetentionPeriodDays { get; set; } = 7;
}

public class InboxCleanupService<TDbContext> : BackgroundService
    where TDbContext : DbContext, IInboxDbContext
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InboxCleanupService<TDbContext>> _logger;
    private readonly InboxCleanupOptions _options;

    public InboxCleanupService(
        IServiceProvider serviceProvider,
        ILogger<InboxCleanupService<TDbContext>> logger,
        IOptions<InboxCleanupOptions> options)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "🧹 InboxCleanupService started - Interval: {Interval} days, Retention: {Retention} days",
            _options.CleanupIntervalDays,
            _options.RetentionPeriodDays);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();

                var cutoffDate = DateTime.UtcNow.AddDays(-_options.RetentionPeriodDays);

                var oldMessages = await dbContext.InboxMessages
                    .Where(m => m.ProcessedOn < cutoffDate)
                    .ToListAsync(stoppingToken);

                if (oldMessages.Any())
                {
                    dbContext.InboxMessages.RemoveRange(oldMessages);
                    await dbContext.SaveChangesAsync(stoppingToken);

                    _logger.LogInformation("🧹 InboxCleanupService removed {Count} old messages (Older than {Date})", oldMessages.Count, cutoffDate);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "❌ Error in InboxCleanupService");
            }

            await Task.Delay(TimeSpan.FromDays(_options.CleanupIntervalDays), stoppingToken);
        }

        _logger.LogInformation("🧹 InboxCleanupService stopped");
    }
}

public static class InboxCleanupServiceExtensions
{
    /// <summary>
    /// InboxCleanupService BackgroundService'ini ve konfigürasyonunu sisteme kaydeder.
    /// Örnek: services.AddInboxCleanupService&lt;MyDbContext&gt;(o => o.RetentionPeriodDays = 14);
    /// </summary>
    public static IServiceCollection AddInboxCleanupService<TDbContext>(
        this IServiceCollection services,
        Action<InboxCleanupOptions>? configure = null)
        where TDbContext : DbContext, IInboxDbContext
    {
        services.Configure<InboxCleanupOptions>(o => configure?.Invoke(o));
        services.AddHostedService<InboxCleanupService<TDbContext>>();
        return services;
    }
}
