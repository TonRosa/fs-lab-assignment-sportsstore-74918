using Shared.Contracts.Enums;

namespace OrderManagement.API.DTOs;

public class OrderDto
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public OrderStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public string? FailureReason { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
    public PaymentRecordDto? PaymentRecord { get; set; }
    public ShipmentRecordDto? ShipmentRecord { get; set; }
    public InventoryRecordDto? InventoryRecord { get; set; }
}

public class OrderItemDto
{
    public Guid Id { get; set; }
    public long ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}

public class PaymentRecordDto
{
    public bool IsApproved { get; set; }
    public string? TransactionId { get; set; }
    public string? FailureReason { get; set; }
    public DateTime ProcessedAt { get; set; }
}

public class ShipmentRecordDto
{
    public string TrackingNumber { get; set; } = string.Empty;
    public DateTime EstimatedDispatch { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class InventoryRecordDto
{
    public bool IsConfirmed { get; set; }
    public string? FailureReason { get; set; }
    public DateTime ProcessedAt { get; set; }
}