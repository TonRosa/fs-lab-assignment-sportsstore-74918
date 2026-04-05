using MediatR;
using OrderManagement.API.DTOs;

namespace OrderManagement.API.CQRS.Commands;

public record CheckoutOrderCommand(CreateOrderDto Order) : IRequest<OrderDto>;

public class CheckoutOrderCommandHandler : IRequestHandler<CheckoutOrderCommand, OrderDto>
{
    private readonly OrderManagement.API.Data.OrderDbContext _db;
    private readonly OrderManagement.API.Messaging.IRabbitMqPublisher _publisher;
    private readonly AutoMapper.IMapper _mapper;

    public CheckoutOrderCommandHandler(
        OrderManagement.API.Data.OrderDbContext db,
        OrderManagement.API.Messaging.IRabbitMqPublisher publisher,
        AutoMapper.IMapper mapper)
    {
        _db = db;
        _publisher = publisher;
        _mapper = mapper;
    }

    public async Task<OrderDto> Handle(CheckoutOrderCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Order;

        // Get products to calculate prices
        var productIds = dto.Items.Select(i => i.ProductId).ToList();
        var products = _db.Products
            .Where(p => productIds.Contains(p.Id))
            .ToList();

        // Build order items
        var items = dto.Items.Select(i =>
        {
            var product = products.First(p => p.Id == i.ProductId);
            return new OrderManagement.API.Models.OrderItem
            {
                ProductId = i.ProductId,
                ProductName = product.Name,
                Quantity = i.Quantity,
                UnitPrice = product.Price
            };
        }).ToList();

        // Create order
        var order = new OrderManagement.API.Models.Order
        {
            CustomerId = dto.CustomerId,
            Items = items,
            TotalAmount = items.Sum(i => i.Quantity * i.UnitPrice),
            ShippingName = dto.ShippingAddress.FullName,
            ShippingLine1 = dto.ShippingAddress.Line1,
            ShippingLine2 = dto.ShippingAddress.Line2,
            ShippingCity = dto.ShippingAddress.City,
            ShippingState = dto.ShippingAddress.State,
            ShippingCountry = dto.ShippingAddress.Country,
            ShippingZip = dto.ShippingAddress.Zip
        };

        _db.Orders.Add(order);
        await _db.SaveChangesAsync(cancellationToken);

        // Publish to RabbitMQ
        var event_ = new Shared.Contracts.Events.OrderSubmitted
        {
            OrderId = order.Id,
            CustomerId = order.CustomerId,
            CorrelationId = order.CorrelationId,
            TotalAmount = order.TotalAmount,
            Items = items.Select(i => new Shared.Contracts.DTOs.OrderItemDto
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList(),
            ShippingAddress = new Shared.Contracts.DTOs.ShippingAddressDto
            {
                FullName = order.ShippingName,
                Line1 = order.ShippingLine1,
                Line2 = order.ShippingLine2,
                City = order.ShippingCity,
                State = order.ShippingState,
                Country = order.ShippingCountry,
                Zip = order.ShippingZip
            }
        };

        await _publisher.PublishAsync(event_,
            OrderManagement.API.Messaging.QueueNames.OrderSubmitted);

        Serilog.Log.Information(
            "[OrderAPI] Order {OrderId} submitted for customer {CustomerId}",
            order.Id, order.CustomerId);

        // Return with customer info
        var result = _mapper.Map<OrderDto>(order);
        result.CustomerName = _db.Customers
            .Where(c => c.Id == order.CustomerId)
            .Select(c => c.Name)
            .FirstOrDefault() ?? "Unknown";

        return result;
    }
}