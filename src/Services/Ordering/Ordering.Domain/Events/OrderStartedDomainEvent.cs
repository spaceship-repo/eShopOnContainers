
namespace Microsoft.eShopOnContainers.Services.Ordering.Domain.Events
{
    /// <summary>
    /// Event used when an order is created
    /// </summary>
    public class OrderStartedDomainEvent : INotification
    {
        public string UserId { get; }
        public string UserName { get; }
        public int CardTypeId { get; }
        public string CardNumber { get; }
        public string CardSecurityNumber { get; }
        public string CardHolderName { get; }
        public string StripeCustomerId { get; }
        public string StripePaymentMethodId { get; }

        public Decimal TotalAmount { get; }
        public DateTime CardExpiration { get; }
        public Order Order { get; }

        public OrderStartedDomainEvent(Order order, string userId, string userName,
                                       int cardTypeId, string cardNumber,
                                       string cardSecurityNumber, string cardHolderName,
                                       DateTime cardExpiration, string stripeCustomerId, string stripePaymentMethodId, decimal totalAmount)
        {
            Order = order;
            UserId = userId;
            UserName = userName;
            CardTypeId = cardTypeId;
            CardNumber = cardNumber;
            CardSecurityNumber = cardSecurityNumber;
            CardHolderName = cardHolderName;
            CardExpiration = cardExpiration;
            StripeCustomerId = stripeCustomerId;
            StripePaymentMethodId = stripePaymentMethodId;
            TotalAmount = totalAmount;
        }
    }
}
