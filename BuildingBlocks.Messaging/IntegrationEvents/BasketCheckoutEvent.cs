using System;
using System.Collections.Generic;
using System.Text;

namespace BuildingBlocks.Messaging.IntegrationEvents;

public class BasketCheckoutEvent
{
    public string UserName { get; set; }
    public decimal TotalPrice { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string EmailAddress { get; set; }
    public string AddressLine { get; set; }
    public string Country { get; set; }
    public string State { get; set; }
    public string ZipCode { get; set; }
    public string CardName { get; set; }
    public string CardNumber { get; set; }
    public string Expiration { get; set; }
    public string CVV { get; set; }
    public Guid BuyerId { get; set; }

    public List<BasketItemMessage> BasketItems { get; set; }
}

public class BasketItemMessage
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}
