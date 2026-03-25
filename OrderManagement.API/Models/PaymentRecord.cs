namespace OrderManagement.API.Models
{
    public class PaymentRecord
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid OrderId { get; set; }
        public Order Order { get; set; } = null!;
        public bool IsApproved { get; set; }
        public string? TransactionId { get; set; }
        public string? FailureReason { get; set; }
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    }
}
