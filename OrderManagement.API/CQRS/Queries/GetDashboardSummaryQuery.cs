using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderManagement.API.Data;
using Shared.Contracts.Enums;

namespace OrderManagement.API.CQRS.Queries;

public record GetDashboardSummaryQuery : IRequest<DashboardSummaryDto>;

public class DashboardSummaryDto
{
    public int TotalOrders { get; set; }
    public int CompletedOrders { get; set; }
    public int FailedOrders { get; set; }
    public int PendingOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TotalProducts { get; set; }
    public int TotalCustomers { get; set; }
    public List<StatusCountDto> OrdersByStatus { get; set; } = new();
}

public class StatusCountDto
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class GetDashboardSummaryQueryHandler
    : IRequestHandler<GetDashboardSummaryQuery, DashboardSummaryDto>
{
    private readonly OrderDbContext _db;

    public GetDashboardSummaryQueryHandler(OrderDbContext db)
    {
        _db = db;
    }

    public async Task<DashboardSummaryDto> Handle(
        GetDashboardSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var orders = await _db.Orders.ToListAsync(cancellationToken);
        var failedStatuses = new[]
        {
            OrderStatus.Failed,
            OrderStatus.PaymentFailed,
            OrderStatus.InventoryFailed
        };

        return new DashboardSummaryDto
        {
            TotalOrders = orders.Count,
            CompletedOrders = orders.Count(o => o.Status == OrderStatus.Completed),
            FailedOrders = orders.Count(o => failedStatuses.Contains(o.Status)),
            PendingOrders = orders.Count(o =>
                o.Status != OrderStatus.Completed &&
                !failedStatuses.Contains(o.Status)),
            TotalRevenue = orders
                .Where(o => o.Status == OrderStatus.Completed)
                .Sum(o => o.TotalAmount),
            TotalProducts = await _db.Products.CountAsync(cancellationToken),
            TotalCustomers = await _db.Customers.CountAsync(cancellationToken),
            OrdersByStatus = orders
                .GroupBy(o => o.Status.ToString())
                .Select(g => new StatusCountDto
                {
                    Status = g.Key,
                    Count = g.Count()
                })
                .ToList()
        };
    }
}