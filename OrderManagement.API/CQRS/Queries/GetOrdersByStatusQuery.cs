using MediatR;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using OrderManagement.API.DTOs;
using OrderManagement.API.Data;
using Shared.Contracts.Enums;

namespace OrderManagement.API.CQRS.Queries;

public record GetOrdersByStatusQuery(OrderStatus Status) : IRequest<List<OrderDto>>;

public class GetOrdersByStatusQueryHandler
    : IRequestHandler<GetOrdersByStatusQuery, List<OrderDto>>
{
    private readonly OrderDbContext _db;
    private readonly IMapper _mapper;

    public GetOrdersByStatusQueryHandler(OrderDbContext db, IMapper mapper)
    {
        _db = db;
        _mapper = mapper;
    }

    public async Task<List<OrderDto>> Handle(
        GetOrdersByStatusQuery request,
        CancellationToken cancellationToken)
    {
        var orders = await _db.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items)
            .Include(o => o.PaymentRecord)
            .Include(o => o.ShipmentRecord)
            .Include(o => o.InventoryRecord)
            .Where(o => o.Status == request.Status)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);

        return _mapper.Map<List<OrderDto>>(orders);
    }
}