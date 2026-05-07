using System;
using System.Collections.Generic;
using System.Text;

namespace BuildingBlocks.Messaging.IntegrationEvents;

public class StockReservedEvent
{
    public Guid OrderId { get; set; }

    public StockReservedEvent(Guid orderId)
    {
        OrderId = orderId;
    }
}
