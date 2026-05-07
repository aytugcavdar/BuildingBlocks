using BuildingBlocks.Core.Inbox;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Infrastructure.Inbox;

/// <summary>
/// Gelen her mesajın daha önce işlenip işlenmediğini (InboxMessage tablosunu) kontrol eden filtre.
/// Idempotency sağlar.
/// </summary>
public class IdempotentConsumeFilter<TMessage> : IFilter<ConsumeContext<TMessage>>
    where TMessage : class
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<IdempotentConsumeFilter<TMessage>> _logger;

    public IdempotentConsumeFilter(IServiceProvider serviceProvider, ILogger<IdempotentConsumeFilter<TMessage>> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task Send(ConsumeContext<TMessage> context, IPipe<ConsumeContext<TMessage>> next)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IInboxDbContext>();

        var messageId = context.MessageId ?? Guid.NewGuid();
        var messageName = typeof(TMessage).Name;

        // Mesaj daha önce işlenmiş mi?
        var alreadyProcessed = await dbContext.InboxMessages
             .AnyAsync(m => m.Id == messageId, context.CancellationToken);

        if (alreadyProcessed)
        {
            _logger.LogInformation("Inbox: Mesaj (Id: {MessageId}, Type: {MessageType}) daha önce işlendiği için yoksayıldı.", messageId, messageName);
            return; // Pipeline'ı burada kesiyoruz, sonraki adıma (Tüketiciye) gitmiyor.
        }

        // Tüketiciye iletip iş mantığının bitmesini BEKLE
        await next.Send(context);

        // İşlem başarılı olduktan sonra Inbox'a kaydet
        dbContext.InboxMessages.Add(new InboxMessage
        {
            Id = messageId,
            Name = messageName,
            ProcessedOn = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync(context.CancellationToken);
        _logger.LogInformation("Inbox: Mesaj (Id: {MessageId}, Type: {MessageType}) başarıyla işlendi ve Inbox'a kaydedildi.", messageId, messageName);
    }

    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("idempotentConsumeFilter");
    }
}
