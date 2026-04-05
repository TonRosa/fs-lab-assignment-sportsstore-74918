using MediatR;
using Shared.Contracts.Enums;

namespace OrderManagement.API.CQRS.Commands;

public record UpdateOrderStatusCommand(
    Guid OrderId,
    OrderStatus NewStatus,
    string? FailureReason = null) : IRequest<bool>;

public class UpdateOrderStatusCommandHandler
    : IRequestHandler<UpdateOrderStatusCommand, bool>
{
    private readonly OrderManagement.API.Data.OrderDbContext _db;

    public UpdateOrderStatusCommandHandler(
        OrderManagement.API.Data.OrderDbContext db)
    {
        _db = db;
    }

    public async Task<bool> Handle(
        UpdateOrderStatusCommand request,
        CancellationToken cancellationToken)
    {
        var order = await _db.Orders.FindAsync(request.OrderId);
        if (order == null) return false;

        order.Status = request.NewStatus;
        order.UpdatedAt = DateTime.UtcNow;

        if (request.FailureReason != null)
            order.FailureReason = request.FailureReason;

        await _db.SaveChangesAsync(cancellationToken);

        Serilog.Log.Information(
            "[OrderAPI] Order {OrderId} status updated to {Status}",
            request.OrderId, request.NewStatus);

        return true;
    }
}