namespace OrderManagement.API.Models
{
    public class ShipmentRecord
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid OrderId { get; set; }
        public Order Order { get; set; } = null!;
        public string TrackingNumber { get; set; } = string.Empty;
        public DateTime EstimatedDispatch { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? FailureReason { get; set; }
    }
}
