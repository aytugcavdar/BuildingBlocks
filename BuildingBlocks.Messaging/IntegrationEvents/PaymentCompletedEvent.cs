using System;
using System.Collections.Generic;
using System.Text;

namespace BuildingBlocks.Messaging.IntegrationEvents;

public class PaymentCompletedEvent
{
    public Guid OrderId { get; set; }

}
