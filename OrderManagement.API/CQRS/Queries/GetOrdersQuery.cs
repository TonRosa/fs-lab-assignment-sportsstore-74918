using MediatR;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using OrderManagement.API.DTOs;
using OrderManagement.API.Data;
using Shared.Contracts.Enums;

namespace OrderManagement.API.CQRS.Queries;

public record GetOrdersQuery(OrderStatus? Status = null) : IRequest<List<OrderDto>>;

public class GetOrdersQueryHandler
    : IRequestHandler<GetOrdersQuery, List<OrderDto>>
{
    private readonly OrderDbContext _db;
    private readonly IMapper _mapper;

    public GetOrdersQueryHandler(OrderDbContext db, IMapper mapper)
    {
        _db = db;
        _mapper = mapper;
    }

    public async Task<List<OrderDto>> Handle(
        GetOrdersQuery request,
        CancellationToken cancellationToken)
    {
        var query = _db.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items)
            .Include(o => o.PaymentRecord)
            .Include(o => o.ShipmentRecord)
            .Include(o => o.InventoryRecord)
            .AsQueryable();

        if (request.Status.HasValue)
            query = query.Where(o => o.Status == request.Status.Value);

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);

        return _mapper.Map<List<OrderDto>>(orders);
    }
}