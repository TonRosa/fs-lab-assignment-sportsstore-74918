using Microsoft.Extensions.Options;
using SportsStore.Models;
using Stripe;
using Stripe.Checkout;

namespace SportsStore.Services
{
    public class StripePaymentService : IPaymentService
    {
        private readonly ILogger<StripePaymentService> _logger;
        private readonly StripeSettings _settings;

        public StripePaymentService(IOptions<StripeSettings> settings, ILogger<StripePaymentService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
            StripeConfiguration.ApiKey = _settings.SecretKey;
        }
        public async Task<PaymentResult> ProcessPayment(PaymentRequest request)
        {
            try
            {
                _logger.LogInformation("Processing payment for order {OrderId}, amount {Amount}",
                    request.OrderId, request.Amount);

                var options = new PaymentIntentCreateOptions
                {
                    Amount = (long)(request.Amount * 100), // Convert to cents
                    Currency = request.Currency ?? "usd",
                    PaymentMethodTypes = new List<string> { "card" },
                    ReceiptEmail = request.CustomerEmail,
                    Metadata = request.Metadata ?? new Dictionary<string, string>()
                };

                // Add order ID to metadata if not already present
                if (!options.Metadata.ContainsKey("OrderId"))
                {
                    options.Metadata["OrderId"] = request.OrderId.ToString();
                }

                var service = new PaymentIntentService();
                var intent = await service.CreateAsync(options);

                _logger.LogInformation("Payment successful for order {OrderId}, Transaction: {TransactionId}",
                    request.OrderId, intent.Id);

                return new PaymentResult
                {
                    Success = true,
                    TransactionId = intent.Id,
                    ClientSecret = intent.ClientSecret,
                    Status = PaymentStatus.Succeeded
                };
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe payment failed for order {OrderId}", request.OrderId);

                return new PaymentResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    Status = PaymentStatus.Failed
                };
            }
        }
        // Method 1: Using Payment Intents (better for your custom checkout form)
        public async Task<PaymentResult> CreatePaymentIntent(decimal amount, string orderId)
        {
            try
            {
                var options = new PaymentIntentCreateOptions
                {
                    Amount = (long)(amount * 100),
                    Currency = "usd",
                    PaymentMethodTypes = new List<string> { "card" },
                    Metadata = new Dictionary<string, string>
                    {
                        ["OrderId"] = orderId
                    }
                };

                var service = new PaymentIntentService();
                var intent = await service.CreateAsync(options);

                return new PaymentResult
                {
                    Success = true,
                    TransactionId = intent.Id,
                    ClientSecret = intent.ClientSecret
                };
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe error");
                return new PaymentResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        // Method 2: Using Checkout Sessions (simpler, Stripe hosts the payment page)
        public string CreateCheckoutSession(Cart cart, string successUrl, string cancelUrl)
        {
            var lineItems = cart.Lines.Select(line => new SessionLineItemOptions
            {
                PriceData = new SessionLineItemPriceDataOptions
                {
                    Currency = "usd",
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = line.Product.Name,
                        Description = line.Product.Description
                    },
                    UnitAmount = (long)(line.Product.Price * 100)
                },
                Quantity = line.Quantity
            }).ToList();

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = lineItems,
                Mode = "payment",
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                Metadata = new Dictionary<string, string>
                {
                    ["OrderId"] = Guid.NewGuid().ToString() // You'd use your actual order ID
                }
            };

            var service = new SessionService();
            var session = service.Create(options);

            return session.Id;
        }
    }
}