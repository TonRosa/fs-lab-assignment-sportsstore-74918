namespace SportsStore.Models

{
    public interface IPaymentService
    {
        Task<PaymentResult> ProcessPayment(PaymentRequest request);

    }
}
