using System;
using System.Collections.Generic;
using System.Text;

namespace BuildingBlocks.Messaging.IntegrationEvents;


public class UserCreatedIntegrationEvent
{
    public Guid UserId { get; set; }
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string UserName { get; set; }
    public string? EmailConfirmationToken { get; set; }
    public DateTime OccurredOn { get; set; }
}
