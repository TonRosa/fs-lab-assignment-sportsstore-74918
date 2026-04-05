using MediatR;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using OrderManagement.API.DTOs;
using OrderManagement.API.Data;

namespace OrderManagement.API.CQRS.Queries;

public record GetOrderByIdQuery(Guid OrderId) : IRequest<OrderDto?>;

public class GetOrderByIdQueryHandler
    : IRequestHandler<GetOrderByIdQuery, OrderDto?>
{
    private readonly OrderDbContext _db;
    private readonly IMapper _mapper;

    public GetOrderByIdQueryHandler(OrderDbContext db, IMapper mapper)
    {
        _db = db;
        _mapper = mapper;
    }

    public async Task<OrderDto?> Handle(
        GetOrderByIdQuery request,
        CancellationToken cancellationToken)
    {
        var order = await _db.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items)
            .Include(o => o.PaymentRecord)
            .Include(o => o.ShipmentRecord)
            .Include(o => o.InventoryRecord)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

        return order == null ? null : _mapper.Map<OrderDto>(order);
    }
}