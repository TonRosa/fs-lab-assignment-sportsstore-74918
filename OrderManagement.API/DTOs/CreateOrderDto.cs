namespace OrderManagement.API.DTOs;

public class CreateOrderDto
{
    public Guid CustomerId { get; set; }
    public List<CreateOrderItemDto> Items { get; set; } = new();
    public ShippingAddressInputDto ShippingAddress { get; set; } = new();
}

public class CreateOrderItemDto
{
    public long ProductId { get; set; }
    public int Quantity { get; set; }
}

public class ShippingAddressInputDto
{
    public string FullName { get; set; } = string.Empty;
    public string Line1 { get; set; } = string.Empty;
    public string? Line2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string? Zip { get; set; }
}