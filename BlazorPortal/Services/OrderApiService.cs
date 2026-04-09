using System.Net.Http.Json;
using Shared.Contracts.Enums;

namespace BlazorPortal.Services;

public class ProductDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
    public int Stock { get; set; }
}

public class OrderDto
{
    public Guid Id { get; set; }
    public string ShortId { get; set; } = string.Empty;
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

public class OrderItemDto
{
    public long ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}

public class CustomerDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class CartItem
{
    public ProductDto Product { get; set; } = new();
    public int Quantity { get; set; }
}

public class OrderApiService
{
    private readonly HttpClient _http;

    public OrderApiService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<ProductDto>> GetProductsAsync()
    {
        return await _http.GetFromJsonAsync<List<ProductDto>>("api/products")
               ?? new List<ProductDto>();
    }

    public async Task<List<OrderDto>> GetCustomerOrdersAsync(Guid customerId)
    {
        return await _http.GetFromJsonAsync<List<OrderDto>>(
            $"api/customers/{customerId}/orders")
               ?? new List<OrderDto>();
    }

    public async Task<List<OrderDto>> SearchOrdersByEmailAsync(string email)
    {
        return await _http.GetFromJsonAsync<List<OrderDto>>(
            $"api/customers/search?email={email}")
               ?? new List<OrderDto>();
    }
    public async Task<OrderDto?> GetOrderAsync(Guid orderId)
    {
        return await _http.GetFromJsonAsync<OrderDto>($"api/orders/{orderId}");
    }

    public async Task<CustomerDto?> CreateCustomerAsync(
        string name, string email)
    {
        var response = await _http.PostAsJsonAsync("api/customers",
            new { name, email });
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<CustomerDto>();
        return null;
    }
    public async Task<List<OrderDto>> GetAllOrdersAsync()
    {
        return await _http.GetFromJsonAsync<List<OrderDto>>("api/orders")
               ?? new List<OrderDto>();
    }

    public async Task<OrderDto?> CheckoutAsync(
        Guid customerId,
        List<CartItem> cartItems,
        ShippingAddressInput address)
    {
        var payload = new
        {
            customerId,
            items = cartItems.Select(i => new
            {
                productId = i.Product.Id,
                quantity = i.Quantity
            }),
            shippingAddress = address
        };

        var response = await _http.PostAsJsonAsync("api/orders/checkout", payload);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<OrderDto>();
        return null;
    }
}

public class ShippingAddressInput
{
    public string FullName { get; set; } = string.Empty;
    public string Line1 { get; set; } = string.Empty;
    public string? Line2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string? Zip { get; set; }
}