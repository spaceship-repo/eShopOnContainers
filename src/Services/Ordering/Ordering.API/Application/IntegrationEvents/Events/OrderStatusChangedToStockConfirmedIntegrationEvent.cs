namespace Microsoft.eShopOnContainers.Services.Ordering.API.Application.IntegrationEvents.Events;

public record OrderStatusChangedToStockConfirmedIntegrationEvent : IntegrationEvent
{
    public int OrderId { get; }
    public string OrderStatus { get; }
    public string BuyerName { get; }
     
    public string PaymentMethod { get; }

    public string OrderItems { get; }

    public OrderStatusChangedToStockConfirmedIntegrationEvent(int orderId,  string orderStatus, string buyerName, string paymentMethod, string orderItems)
    {
        OrderId = orderId;
        OrderStatus = orderStatus;
        BuyerName = buyerName;
        PaymentMethod = paymentMethod;
        OrderItems = orderItems;
    }
}
