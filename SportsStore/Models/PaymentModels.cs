namespace SportsStore.Models;

public class PaymentRequest
{
    public int OrderId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "usd";
    public string CustomerEmail { get; set; } = string.Empty;
    public string PaymentMethodId { get; set; } = string.Empty;
    public Dictionary<string, string> Metadata { get; set; } = new();
}

public class PaymentResult
{
    public bool Success { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public PaymentStatus Status { get; set; }
}

public enum PaymentStatus
{
    Pending,
    Succeeded,
    Failed,
    Refunded
}