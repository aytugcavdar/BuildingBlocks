namespace BuildingBlocks.Messaging.IntegrationEvents;

public class PaymentFailedEvent
{
    public Guid OrderId { get; set; }
    public string ErrorMessage { get; set; }
    public List<OrderItemMessage> OrderItems { get; set; }
}