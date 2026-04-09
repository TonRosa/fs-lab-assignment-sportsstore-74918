using MediatR;
using Shared.Contracts.Enums;
using Serilog;

namespace OrderManagement.API.CQRS.Commands;

public record CancelOrderCommand(Guid OrderId) : IRequest<bool>;

public class CancelOrderCommandHandler
    : IRequestHandler<CancelOrderCommand, bool>
{
    private readonly OrderManagement.API.Data.OrderDbContext _db;

    public CancelOrderCommandHandler(
        OrderManagement.API.Data.OrderDbContext db)
    {
        _db = db;
    }

    public async Task<bool> Handle(
        CancelOrderCommand request,
        CancellationToken cancellationToken)
    {
        var order = await _db.Orders.FindAsync(request.OrderId);
        if (order == null) return false;

        // Can only cancel if not completed
        if (order.Status == OrderStatus.Completed)
            return false;

        order.Status = OrderStatus.Failed;
        order.FailureReason = "Cancelled by customer";
        order.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        Log.Information(
            "[OrderAPI] Order {OrderId} cancelled by customer",
            request.OrderId);

        return true;
    }
}