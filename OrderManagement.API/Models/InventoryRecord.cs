namespace OrderManagement.API.Models
{
    public class InventoryRecord
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid OrderId { get; set; }
        public Order Order { get; set; } = null!;
        public bool IsConfirmed { get; set; }
        public string? FailureReason { get; set; }
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    }
}
