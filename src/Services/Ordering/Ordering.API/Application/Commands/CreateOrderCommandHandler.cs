namespace Microsoft.eShopOnContainers.Services.Ordering.API.Application.Commands;

using Microsoft.eShopOnContainers.Services.Ordering.Domain.AggregatesModel.OrderAggregate;
using Stripe;

// Regular CommandHandler
public class CreateOrderCommandHandler
    : IRequestHandler<CreateOrderCommand, bool>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IIdentityService _identityService;
    private readonly IMediator _mediator;
    private readonly IOrderingIntegrationEventService _orderingIntegrationEventService;
    private readonly ILogger<CreateOrderCommandHandler> _logger;

    // Using DI to inject infrastructure persistence Repositories
    public CreateOrderCommandHandler(IMediator mediator,
        IOrderingIntegrationEventService orderingIntegrationEventService,
        IOrderRepository orderRepository,
        IIdentityService identityService,
        ILogger<CreateOrderCommandHandler> logger)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _identityService = identityService ?? throw new ArgumentNullException(nameof(identityService));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _orderingIntegrationEventService = orderingIntegrationEventService ?? throw new ArgumentNullException(nameof(orderingIntegrationEventService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> Handle(CreateOrderCommand message, CancellationToken cancellationToken)
    {
        var customerSearchOptions = new CustomerSearchOptions
        {
            Query = $"email:'{message.UserEmail}'",
        };
        var customerService = new CustomerService();
        var customerSearchResult = await customerService.SearchAsync(customerSearchOptions);
        var existingCustomer = customerSearchResult.Data.FirstOrDefault();
        if (existingCustomer == null)
        {
            var customerOption = new CustomerCreateOptions
            {
                Address = new AddressOptions
                {
                    City = message.City,
                    Country = message.Country,
                    Line1 = message.Street,
                    State = message.State,
                    PostalCode = message.ZipCode
                },
                Email = message.UserEmail
            };

            existingCustomer = await customerService.CreateAsync(customerOption);
        }

        var payementMethodsOptions = new PaymentMethodListOptions
        {
            Type = "card",
            Customer = existingCustomer.Id
        };
        var payementMethodsService = new PaymentMethodService();
        var paymentMethods = await payementMethodsService.ListAsync(payementMethodsOptions);
        var existingPaymentMethod= paymentMethods.FirstOrDefault(x => x.Card.ExpMonth == message.CardExpiration.Month
                && x.Card.ExpYear == message.CardExpiration.Year);

        if (existingPaymentMethod == null)
        {
            var createPaymentMethodOption = new PaymentMethodCreateOptions
            {
                Card = new PaymentMethodCardOptions
                {
                    Number = message.CardNumber,
                    ExpMonth = message.CardExpiration.Month,
                    ExpYear = message.CardExpiration.Year,
                    Cvc = message.CardSecurityNumber
                },
                Type = "card"
            };
            existingPaymentMethod = await payementMethodsService.CreateAsync(createPaymentMethodOption);
            await payementMethodsService.AttachAsync(existingPaymentMethod.Id, new PaymentMethodAttachOptions { Customer = existingCustomer.Id });
        }        

        // Add Integration event to clean the basket
        var orderStartedIntegrationEvent = new OrderStartedIntegrationEvent(message.UserId);
        await _orderingIntegrationEventService.AddAndSaveEventAsync(orderStartedIntegrationEvent);

        // Add/Update the Buyer AggregateRoot
        // DDD patterns comment: Add child entities and value-objects through the Order Aggregate-Root
        // methods and constructor so validations, invariants and business logic 
        // make sure that consistency is preserved across the whole aggregate
        var address = new Domain.AggregatesModel.OrderAggregate.Address(message.Street, message.City, message.State, message.Country, message.ZipCode);
        var order = new Domain.AggregatesModel.OrderAggregate.Order(message.UserId, message.UserName, address, message.CardTypeId, message.CardNumber, message.CardSecurityNumber, message.CardHolderName, message.CardExpiration, existingCustomer.Id, existingPaymentMethod.Id);
        
        foreach (var item in message.OrderItems)
        {
            order.AddOrderItem(item.ProductId, item.ProductName, item.UnitPrice, item.Discount, item.PictureUrl, item.Units);
        }

        

        _logger.LogInformation("----- Creating Order - Order: {@Order}", order);

        _orderRepository.Add(order);

        return await _orderRepository.UnitOfWork
            .SaveEntitiesAsync(cancellationToken);
    }
}


// Use for Idempotency in Command process
public class CreateOrderIdentifiedCommandHandler : IdentifiedCommandHandler<CreateOrderCommand, bool>
{
    public CreateOrderIdentifiedCommandHandler(
        IMediator mediator,
        IRequestManager requestManager,
        ILogger<IdentifiedCommandHandler<CreateOrderCommand, bool>> logger)
        : base(mediator, requestManager, logger)
    {
    }

    protected override bool CreateResultForDuplicateRequest()
    {
        return true;                // Ignore duplicate requests for creating order.
    }
}
