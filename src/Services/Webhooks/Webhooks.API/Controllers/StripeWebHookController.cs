using Stripe;
using Stripe.Checkout;
using System.IO;

namespace Webhooks.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class StripeWebHookController : ControllerBase
    {
        // If you are testing your webhook locally with the Stripe CLI you
        // can find the endpoint's secret by running `stripe listen`
        // Otherwise, find your endpoint's secret in your webhook
        // settings in the Developer Dashboard

        private readonly IIdentityService _identityService;
        private readonly IEventBus _eventBus;
        private readonly ILogger<StripeWebHookController> _logger;
        private readonly IConfiguration Configuration;

        public StripeWebHookController(
            ILogger<StripeWebHookController> logger,
            IIdentityService identityService,
            IEventBus eventBus,
            IConfiguration configuration)
        {
            _logger = logger;
            _identityService = identityService;
            _eventBus = eventBus;
            Configuration = configuration;
        }

        [HttpPost]
        [Route("Index")]
        [AllowAnonymous]
        public async Task<IActionResult> StripeWebHook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

            try
            {
                var stripeEvent = EventUtility.ParseEvent(json);

                //verify event
                var signatureHeader = Request.Headers["Stripe-Signature"];
                stripeEvent = EventUtility.ConstructEvent(json,
                        signatureHeader, Configuration["StripeConfiguration:StripeWebHookEndpointSecret"]);

                // Handle the event
                if (stripeEvent.Type == Stripe.Events.PaymentIntentSucceeded)
                {
                    var message = stripeEvent.Data.Object as PaymentIntent;
                    var orderId = message.Metadata.GetValueOrDefault("OrderId");
                    var eventMessage = new OrderPaymentSucceededIntegrationEvent(int.Parse(orderId));
                    _eventBus.Publish(eventMessage);
                    _logger.LogInformation($"{DateTime.Now} PaymentIntent was successful!");
                }
                else if (stripeEvent.Type == Stripe.Events.PaymentMethodAttached)
                {
                    var paymentMethod = stripeEvent.Data.Object as PaymentMethod;
                    _logger.LogInformation($"{DateTime.Now} PaymentMethod was attached to a Customer!");
                }
                // ... handle other event types
                else
                {
                    _logger.LogInformation($"{DateTime.Now} Unhandled event type: {0}", stripeEvent.Type);
                }

                return Ok();
            }
            catch (StripeException ex)
            {
                _logger.LogError($"Error: {0} {DateTime.Now}", ex.Message);
                return BadRequest();
            }
        }
    }
}
