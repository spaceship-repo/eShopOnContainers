namespace Microsoft.eShopOnContainers.Payment.API.IntegrationEvents.Events;
    
public record OrderStatusChangedToStockConfirmedIntegrationEvent : IntegrationEvent
{
    public int OrderId { get; }

    public string PaymentMethod { get; }

    public string OrderItems { get; }

    public OrderStatusChangedToStockConfirmedIntegrationEvent(int orderId, string paymentMethod, string orderItems)
    {
        OrderId = orderId;
        PaymentMethod = paymentMethod;
        OrderItems = orderItems;    
    }
}
