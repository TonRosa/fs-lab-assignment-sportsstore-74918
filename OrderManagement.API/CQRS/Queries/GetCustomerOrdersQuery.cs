using MediatR;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using OrderManagement.API.DTOs;
using OrderManagement.API.Data;

namespace OrderManagement.API.CQRS.Queries;

public record GetCustomerOrdersQuery(Guid CustomerId) : IRequest<List<OrderDto>>;

public class GetCustomerOrdersQueryHandler
    : IRequestHandler<GetCustomerOrdersQuery, List<OrderDto>>
{
    private readonly OrderDbContext _db;
    private readonly IMapper _mapper;

    public GetCustomerOrdersQueryHandler(OrderDbContext db, IMapper mapper)
    {
        _db = db;
        _mapper = mapper;
    }

    public async Task<List<OrderDto>> Handle(
        GetCustomerOrdersQuery request,
        CancellationToken cancellationToken)
    {
        var orders = await _db.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items)
            .Include(o => o.PaymentRecord)
            .Include(o => o.ShipmentRecord)
            .Include(o => o.InventoryRecord)
            .Where(o => o.CustomerId == request.CustomerId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);

        return _mapper.Map<List<OrderDto>>(orders);
    }
}