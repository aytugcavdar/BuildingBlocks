using System;
using System.Collections.Generic;
using System.Text;

namespace BuildingBlocks.Messaging.IntegrationEvents;

public class OrderCreatedEvent
{
    public Guid OrderId { get; set; }
    public Guid BuyerId { get; set; }
    public decimal TotalPrice { get; set; }
    public List<OrderItemMessage> OrderItems { get; set; }

    public OrderCreatedEvent()
    {
        OrderItems = new List<OrderItemMessage>();
    }
}

public class OrderItemMessage
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}



