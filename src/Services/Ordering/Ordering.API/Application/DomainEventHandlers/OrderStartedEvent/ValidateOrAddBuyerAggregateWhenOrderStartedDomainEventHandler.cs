﻿using Stripe;

namespace Microsoft.eShopOnContainers.Services.Ordering.API.Application.DomainEventHandlers.OrderStartedEvent;

public class ValidateOrAddBuyerAggregateWhenOrderStartedDomainEventHandler
                    : INotificationHandler<OrderStartedDomainEvent>
{
    private readonly ILoggerFactory _logger;
    private readonly IBuyerRepository _buyerRepository;
    private readonly IIdentityService _identityService;
    private readonly IOrderingIntegrationEventService _orderingIntegrationEventService;

    public ValidateOrAddBuyerAggregateWhenOrderStartedDomainEventHandler(
        ILoggerFactory logger,
        IBuyerRepository buyerRepository,
        IIdentityService identityService,
        IOrderingIntegrationEventService orderingIntegrationEventService)
    {
        _buyerRepository = buyerRepository ?? throw new ArgumentNullException(nameof(buyerRepository));
        _identityService = identityService ?? throw new ArgumentNullException(nameof(identityService));
        _orderingIntegrationEventService = orderingIntegrationEventService ?? throw new ArgumentNullException(nameof(orderingIntegrationEventService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Handle(OrderStartedDomainEvent orderStartedEvent, CancellationToken cancellationToken)
    {
        var paymentIntentCreateOptions = new PaymentIntentCreateOptions
        {
            Amount = 100,
            Currency = "usd",
            PaymentMethodTypes = new List<string>
                        {
                            "card",
                        },
            Customer = orderStartedEvent.StripeCustomerId,
            PaymentMethod = orderStartedEvent.StripePaymentMethodId,
            Metadata = new Dictionary<string, string>() 
            {
                { "OrderId",  orderStartedEvent.Order.Id.ToString()}, 
            }

        };
        var paymentIntentService = new PaymentIntentService();
        var paymentIntent = await paymentIntentService.CreateAsync(paymentIntentCreateOptions);

        var cardTypeId = (orderStartedEvent.CardTypeId != 0) ? orderStartedEvent.CardTypeId : 1;
        var buyer = await _buyerRepository.FindAsync(orderStartedEvent.UserId);
        bool buyerOriginallyExisted = (buyer == null) ? false : true;

        if (!buyerOriginallyExisted)
        {
            buyer = new Buyer(orderStartedEvent.UserId, orderStartedEvent.UserName);
        }

        buyer.VerifyOrAddPaymentMethod(cardTypeId,
                                        $"Payment Method on {DateTime.UtcNow}",
                                        orderStartedEvent.CardNumber,
                                        orderStartedEvent.CardSecurityNumber,
                                        orderStartedEvent.CardHolderName,
                                        orderStartedEvent.CardExpiration,
                                        orderStartedEvent.Order.Id);

        var buyerUpdated = buyerOriginallyExisted ?
            _buyerRepository.Update(buyer) :
            _buyerRepository.Add(buyer);

        await _buyerRepository.UnitOfWork
            .SaveEntitiesAsync(cancellationToken);

        var orderStatusChangedToSubmittedIntegrationEvent = new OrderStatusChangedToSubmittedIntegrationEvent(orderStartedEvent.Order.Id, orderStartedEvent.Order.OrderStatus.Name, buyer.Name);
        await _orderingIntegrationEventService.AddAndSaveEventAsync(orderStatusChangedToSubmittedIntegrationEvent);
        _logger.CreateLogger<ValidateOrAddBuyerAggregateWhenOrderStartedDomainEventHandler>()
            .LogTrace("Buyer {BuyerId} and related payment method were validated or updated for orderId: {OrderId}.",
                buyerUpdated.Id, orderStartedEvent.Order.Id);
    }
}
