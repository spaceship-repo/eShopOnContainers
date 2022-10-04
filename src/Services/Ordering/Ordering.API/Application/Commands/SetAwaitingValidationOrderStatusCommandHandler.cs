namespace Microsoft.eShopOnContainers.Services.Ordering.API.Application.Commands;

// Regular CommandHandler
public class SetAwaitingValidationOrderStatusCommandHandler : IRequestHandler<SetAwaitingValidationOrderStatusCommand, bool>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<SetAwaitingValidationOrderStatusCommandHandler> _logger;
    public SetAwaitingValidationOrderStatusCommandHandler(IOrderRepository orderRepository, ILogger<SetAwaitingValidationOrderStatusCommandHandler> logger)
    {
        _orderRepository = orderRepository;
        _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handler which processes the command when
    /// graceperiod has finished
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
    public async Task<bool> Handle(SetAwaitingValidationOrderStatusCommand command, CancellationToken cancellationToken)
    {
        var orderToUpdate = await _orderRepository.GetAsync(command.OrderNumber);
        if (orderToUpdate == null)
        {
            return false;
        }
        _logger.LogInformation("----- Start SetAwaitingValidationStatus()");
        orderToUpdate.SetAwaitingValidationStatus();
        _logger.LogInformation("----- End SetAwaitingValidationStatus()", orderToUpdate);
        return await _orderRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);
    }
}


// Use for Idempotency in Command process
public class SetAwaitingValidationIdentifiedOrderStatusCommandHandler : IdentifiedCommandHandler<SetAwaitingValidationOrderStatusCommand, bool>
{
    public SetAwaitingValidationIdentifiedOrderStatusCommandHandler(
        IMediator mediator,
        IRequestManager requestManager,
        ILogger<IdentifiedCommandHandler<SetAwaitingValidationOrderStatusCommand, bool>> logger)
        : base(mediator, requestManager, logger)
    {
    }

    protected override bool CreateResultForDuplicateRequest()
    {
        return true;                // Ignore duplicate requests for processing order.
    }
}
