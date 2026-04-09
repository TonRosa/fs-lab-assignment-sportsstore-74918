using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Contracts.Enums;
using Shared.Contracts.Events;
using Serilog;
using OrderManagement.API.Data;
using OrderManagement.API.Models;

namespace OrderManagement.API.Messaging;

public class OrderStatusListener : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly IConfiguration _configuration;
    private readonly string _serviceName = "OrderManagement.API";
    private IConnection? _connection;
    private IChannel? _channel;

    public OrderStatusListener(
        IServiceProvider services,
        IConfiguration configuration)
    {
        _services = services;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Log.Information("[{Service}] Starting order status listener...",
            _serviceName);

        await ConnectWithRetryAsync(stoppingToken);
        if (_channel == null) return;

        // Declare all result queues
        var queues = new[]
        {
            "inventory.confirmed",
            "inventory.failed",
            "payment.approved",
            "payment.rejected",
            "shipping.created",
            "shipping.failed"
        };

        foreach (var queue in queues)
        {
            await _channel.QueueDeclareAsync(
                queue: queue,
                durable: true,
                exclusive: false,
                autoDelete: false);
        }

        await _channel.BasicQosAsync(0, 1, false);

        // Listen to inventory confirmed
        await StartConsumerAsync("inventory.confirmed", async (json) =>
        {
            var msg = JsonSerializer.Deserialize<InventoryConfirmed>(json);
            if (msg == null) return;
            await UpdateOrderAsync(msg.OrderId, OrderStatus.InventoryConfirmed);
            await UpdateInventoryRecordAsync(msg.OrderId, true, null);
            Log.Information(
     "[{Service}] Order {OrderId} inventory confirmed CorrelationId {CorrelationId}",
     _serviceName, msg.OrderId, msg.CorrelationId);
        }, stoppingToken);

        // Listen to inventory failed
        await StartConsumerAsync("inventory.failed", async (json) =>
        {
            var msg = JsonSerializer.Deserialize<InventoryFailed>(json);
            if (msg == null) return;
            await UpdateOrderAsync(msg.OrderId, OrderStatus.InventoryFailed,
                msg.Reason);
            await UpdateInventoryRecordAsync(msg.OrderId, false, msg.Reason);
            Log.Warning(
                "[{Service}] Order {OrderId} inventory failed: {Reason}{CorrelationId}",
                _serviceName, msg.OrderId, msg.Reason, msg.CorrelationId);
        }, stoppingToken);

        // Listen to payment approved
        await StartConsumerAsync("payment.approved", async (json) =>
        {
            var msg = JsonSerializer.Deserialize<PaymentApproved>(json);
            if (msg == null) return;
            await UpdateOrderAsync(msg.OrderId, OrderStatus.PaymentApproved);
            await UpdatePaymentRecordAsync(msg.OrderId, true,
                msg.TransactionId, null);
            Log.Information(
                "[{Service}] Order {OrderId} payment approved{CorrelationId}",
                _serviceName, msg.OrderId, msg.CorrelationId);
        }, stoppingToken);

        // Listen to payment rejected
        await StartConsumerAsync("payment.rejected", async (json) =>
        {
            var msg = JsonSerializer.Deserialize<PaymentRejected>(json);
            if (msg == null) return;
            await UpdateOrderAsync(msg.OrderId, OrderStatus.PaymentFailed,
                msg.Reason);
            await UpdatePaymentRecordAsync(msg.OrderId, false, null, msg.Reason);
            Log.Warning(
                "[{Service}] Order {OrderId} payment rejected: {Reason}{CorrelationId}",
                _serviceName, msg.OrderId, msg.Reason, msg.CorrelationId);
        }, stoppingToken);

        // Listen to shipping created
        await StartConsumerAsync("shipping.created", async (json) =>
        {
            var msg = JsonSerializer.Deserialize<ShippingCreated>(json);
            if (msg == null) return;
            await UpdateOrderAsync(msg.OrderId, OrderStatus.Completed);
            await UpdateShipmentRecordAsync(msg.OrderId,
                msg.TrackingNumber, msg.EstimatedDispatch);
            Log.Information(
                "[{Service}] Order {OrderId} completed! Tracking: {Tracking}{CorrelationId}",
                _serviceName, msg.OrderId, msg.TrackingNumber, msg.CorrelationId);
        }, stoppingToken);

        // Listen to shipping failed
        await StartConsumerAsync("shipping.failed", async (json) =>
        {
            var msg = JsonSerializer.Deserialize<ShippingFailed>(json);
            if (msg == null) return;
            await UpdateOrderAsync(msg.OrderId, OrderStatus.Failed, msg.Reason);
            Log.Warning(
                "[{Service}] Order {OrderId} shipping failed: {Reason}{CorrelationId}",
                _serviceName, msg.OrderId, msg.Reason, msg.CorrelationId);
        }, stoppingToken);

        Log.Information(
            "[{Service}] Listening on all result queues",
            _serviceName);

        while (!stoppingToken.IsCancellationRequested)
            await Task.Delay(1000, stoppingToken);
    }

    private async Task StartConsumerAsync(
        string queue,
        Func<string, Task> handler,
        CancellationToken stoppingToken)
    {
        var consumer = new AsyncEventingBasicConsumer(_channel!);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            var json = Encoding.UTF8.GetString(ea.Body.ToArray());
            try
            {
                await handler(json);
                await _channel!.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                Log.Error(ex,
                    "[{Service}] Error processing {Queue}",
                    _serviceName, queue);
                await _channel!.BasicNackAsync(ea.DeliveryTag, false, false);
            }
        };

        await _channel!.BasicConsumeAsync(
            queue: queue,
            autoAck: false,
            consumer: consumer);
    }

    private async Task UpdateOrderAsync(
        Guid orderId,
        OrderStatus status,
        string? failureReason = null)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        var order = await db.Orders.FindAsync(orderId);
        if (order == null) return;
        order.Status = status;
        order.UpdatedAt = DateTime.UtcNow;
        if (failureReason != null) order.FailureReason = failureReason;
        await db.SaveChangesAsync();
    }

    private async Task UpdateInventoryRecordAsync(
        Guid orderId, bool confirmed, string? reason)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        var record = new InventoryRecord
        {
            OrderId = orderId,
            IsConfirmed = confirmed,
            FailureReason = reason
        };
        db.InventoryRecords.Add(record);
        await db.SaveChangesAsync();
    }

    private async Task UpdatePaymentRecordAsync(
        Guid orderId, bool approved,
        string? transactionId, string? reason)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        var record = new PaymentRecord
        {
            OrderId = orderId,
            IsApproved = approved,
            TransactionId = transactionId,
            FailureReason = reason
        };
        db.PaymentRecords.Add(record);
        await db.SaveChangesAsync();
    }

    private async Task UpdateShipmentRecordAsync(
        Guid orderId, string trackingNumber,
        DateTime estimatedDispatch)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        var record = new ShipmentRecord
        {
            OrderId = orderId,
            TrackingNumber = trackingNumber,
            EstimatedDispatch = estimatedDispatch
        };
        db.ShipmentRecords.Add(record);
        await db.SaveChangesAsync();
    }

    private async Task ConnectWithRetryAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _configuration["RabbitMQ:Host"] ?? "localhost",
            Port = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672"),
            UserName = _configuration["RabbitMQ:Username"] ?? "Sport",
            Password = _configuration["RabbitMQ:Password"] ?? "123"
        };

        var retries = 0;
        while (!stoppingToken.IsCancellationRequested && retries < 10)
        {
            try
            {
                _connection = await factory.CreateConnectionAsync();
                _channel = await _connection.CreateChannelAsync();
                Log.Information(
                    "[{Service}] Connected to RabbitMQ",
                    _serviceName);
                return;
            }
            catch (Exception ex)
            {
                retries++;
                Log.Warning(
                    "[{Service}] RabbitMQ not ready, retry {Retry}/10: {Error}",
                    _serviceName, retries, ex.Message);
                await Task.Delay(3000, stoppingToken);
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel != null) await _channel.CloseAsync();
        if (_connection != null) await _connection.CloseAsync();
        await base.StopAsync(cancellationToken);
    }
}