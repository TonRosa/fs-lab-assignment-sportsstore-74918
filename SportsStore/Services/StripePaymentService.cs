using Microsoft.Extensions.Options;
using SportsStore.Models;
using Stripe;

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
                _logger.LogInformation("Processando pagamento pedido {OrderId}", request.OrderId);

                // PRIMEIRO: Criar um PaymentMethod a partir do token
                var paymentMethodOptions = new PaymentMethodCreateOptions
                {
                    Type = "card",
                    Card = new PaymentMethodCardOptions
                    {
                        Token = request.PaymentMethodId  // O token recebido do frontend
                    }
                };

                var paymentMethodService = new PaymentMethodService();
                var paymentMethod = await paymentMethodService.CreateAsync(paymentMethodOptions);

                // SEGUNDO: Criar o PaymentIntent com o PaymentMethod
                var options = new PaymentIntentCreateOptions
                {
                    Amount = (long)(request.Amount * 100),
                    Currency = "usd",
                    PaymentMethodTypes = new List<string> { "card" },
                    PaymentMethod = paymentMethod.Id,  // Usar o ID do PaymentMethod, não o token
                    Confirm = true,  // Confirmar automaticamente
                    Metadata = new Dictionary<string, string>
                    {
                        ["OrderId"] = request.OrderId.ToString()
                    }
                };

                var service = new PaymentIntentService();
                var intent = await service.CreateAsync(options);

                // Verificar resultado
                if (intent.Status == "succeeded")
                {
                    _logger.LogInformation("Pagamento aprovado! Transação: {TransactionId}", intent.Id);
                    return new PaymentResult
                    {
                        Success = true,
                        TransactionId = intent.Id,
                        Status = PaymentStatus.Succeeded
                    };
                }
                else if (intent.Status == "requires_action" || intent.Status == "requires_confirmation")
                {
                    // Pagamento precisa de autenticação 3D Secure
                    _logger.LogInformation("Pagamento requer autenticação: {Status}", intent.Status);
                    return new PaymentResult
                    {
                        Success = false,
                        Status = PaymentStatus.Pending,
                        ClientSecret = intent.ClientSecret,
                        ErrorMessage = "Autenticação necessária"
                    };
                }
                else
                {
                    _logger.LogWarning("Pagamento {Status} para pedido {OrderId}", intent.Status, request.OrderId);
                    return new PaymentResult
                    {
                        Success = false,
                        Status = PaymentStatus.Failed,
                        ErrorMessage = $"Payment {intent.Status}"
                    };
                }
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "StripeException - Message: {Message}, Type: {Type}, Code: {Code}, StatusCode: {StatusCode}",
                    ex.Message,
                    ex.StripeError?.Type ?? "N/A",
                    ex.StripeError?.Code ?? "N/A",
                    ex.HttpStatusCode);

                return new PaymentResult
                {
                    Success = false,
                    Status = PaymentStatus.Failed,
                    ErrorMessage = ex.Message
                };
            }
        }

        // Método auxiliar para mensagens amigáveis
        private string GetUserFriendlyMessage(StripeException ex)
        {
            return ex.StripeError?.Code switch
            {
                "card_declined" => "Cartão recusado",
                "incorrect_cvc" => "CVC incorreto",
                "expired_card" => "Cartão expirado",
                "processing_error" => "Erro no processamento",
                "insufficient_funds" => "Fundos insuficientes",
                "authentication_required" => "Autenticação necessária",
                _ => "Erro no pagamento: " + ex.Message
            };
        }
    }
    }
