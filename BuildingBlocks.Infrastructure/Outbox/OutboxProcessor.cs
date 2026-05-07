using BuildingBlocks.Core.Outbox;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace BuildingBlocks.Infrastructure.Outbox;

/// <summary>
/// Transactional Outbox mesajlarını işleyen BackgroundService.
/// Periyodik olarak işlenmemiş mesajları okur ve MediatR/mesajlaşma üzerinden publish eder.
/// </summary>
public class OutboxProcessor<TDbContext> : BackgroundService
    where TDbContext : DbContext
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessor<TDbContext>> _logger;
    private readonly OutboxOptions _options;

    public OutboxProcessor(
        IServiceProvider serviceProvider,
        ILogger<OutboxProcessor<TDbContext>> logger,
        IOptions<OutboxOptions> options)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxProcessor started. Polling every {Interval}s", _options.PollingIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingMessagesAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "OutboxProcessor encountered an error");
            }

            await Task.Delay(TimeSpan.FromSeconds(_options.PollingIntervalSeconds), stoppingToken);
        }
    }

    private async Task ProcessPendingMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        var messages = await dbContext.Set<OutboxMessage>()
            .Where(m => m.ProcessedOn == null && m.RetryCount < _options.MaxRetryCount)
            .OrderBy(m => m.OccurredOn)
            .Take(_options.BatchSize)
            .ToListAsync(cancellationToken);

        if (messages.Count == 0) return;

        _logger.LogDebug("Processing {Count} outbox messages", messages.Count);

        foreach (var message in messages)
            await ProcessMessageAsync(message, publisher, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task ProcessMessageAsync(
        OutboxMessage message,
        IPublisher publisher,
        CancellationToken cancellationToken)
    {
        try
        {
            var type = EventTypeRegistry.Dictionary.GetValueOrDefault(message.Type)
                ?? Type.GetType(message.Type);

            if (type == null)
            {
                message.MarkAsFailed($"Unknown event type: {message.Type}");
                _logger.LogWarning("Unknown outbox event type: {Type}", message.Type);
                return;
            }

            var domainEvent = JsonSerializer.Deserialize(message.Content, type);
            if (domainEvent == null)
            {
                message.MarkAsFailed($"Deserialization returned null for type: {message.Type}");
                return;
            }

            await publisher.Publish(domainEvent, cancellationToken);
            message.MarkAsProcessed();

            _logger.LogDebug("Processed outbox message {MessageId} of type {Type}", message.Id, message.Type);
        }
        catch (Exception ex)
        {
            message.MarkAsFailed(ex.Message);
            _logger.LogError(ex, "Failed to process outbox message {MessageId}", message.Id);
        }
    }
}

/// <summary>
/// OutboxProcessor'ı DI'a register etmek için extension method.
/// </summary>
public static class OutboxProcessorExtensions
{
    /// <summary>
    /// OutboxProcessor BackgroundService'i ve OutboxOptions konfigürasyonunu register eder.
    /// Örnek: services.AddOutboxProcessor&lt;MyDbContext&gt;(o => o.BatchSize = 50);
    /// </summary>
    public static IServiceCollection AddOutboxProcessor<TDbContext>(
        this IServiceCollection services,
        Action<OutboxOptions>? configure = null)
        where TDbContext : DbContext
    {
        services.Configure<OutboxOptions>(o => configure?.Invoke(o));
        services.AddHostedService<OutboxProcessor<TDbContext>>();
        return services;
    }
}
