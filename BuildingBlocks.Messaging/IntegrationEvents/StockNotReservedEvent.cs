using System;
using System.Collections.Generic;
using System.Text;

namespace BuildingBlocks.Messaging.IntegrationEvents;

public class StockNotReservedEvent
{
    public Guid OrderId { get; set; }
    public string Reason { get; set; } 

    public StockNotReservedEvent(Guid orderId, string reason)
    {
        OrderId = orderId;
        Reason = reason;
    }
}
