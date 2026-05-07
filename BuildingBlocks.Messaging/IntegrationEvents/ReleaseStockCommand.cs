namespace BuildingBlocks.Messaging.IntegrationEvents;
public class ReleaseStockCommand
{
    public Guid OrderId { get; set; }
    public List<OrderItemMessage> Items { get; set; } = new();

    public ReleaseStockCommand()
    {
    }

    public ReleaseStockCommand(Guid orderId, List<OrderItemMessage> items)
    {
        OrderId = orderId;
        Items = items ?? new List<OrderItemMessage>();
    }
}