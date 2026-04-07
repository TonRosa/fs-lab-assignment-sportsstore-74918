using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Contracts.Events;
using Shared.Contracts.DTOs;
using Serilog;

namespace Inventory.Service;

public class InventoryWorker : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly string _serviceName = "Inventory.Service";
    private IConnection? _connection;
    private IChannel? _channel;

    public InventoryWorker(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Log.Information("[{Service}] Starting...", _serviceName);

        await ConnectWithRetryAsync(stoppingToken);

        if (_channel == null) return;

        // Declare queues
        await _channel.QueueDeclareAsync(
            queue: "order.submitted",
            durable: true, exclusive: false, autoDelete: false);

        await _channel.QueueDeclareAsync(
            queue: "inventory.confirmed",
            durable: true, exclusive: false, autoDelete: false);

        await _channel.QueueDeclareAsync(
            queue: "inventory.failed",
            durable: true, exclusive: false, autoDelete: false);

        await _channel.BasicQosAsync(0, 1, false);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var json = Encoding.UTF8.GetString(body);

            Log.Information(
                "[{Service}] Received message on order.submitted",
                _serviceName);

            try
            {
                var order = JsonSerializer.Deserialize<OrderSubmitted>(json);
                if (order == null) return;

                await ProcessInventoryAsync(order);
                await _channel.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                Log.Error(ex,
                    "[{Service}] Error processing message",
                    _serviceName);
                await _channel.BasicNackAsync(ea.DeliveryTag, false, false);
            }
        };

        await _channel.BasicConsumeAsync(
            queue: "order.submitted",
            autoAck: false,
            consumer: consumer);

        Log.Information(
            "[{Service}] Listening on order.submitted queue",
            _serviceName);

        // Keep running until stopped
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    private async Task ProcessInventoryAsync(OrderSubmitted order)
    {
        Log.Information(
            "[{Service}] Checking inventory for Order {OrderId} " +
            "with {ItemCount} items",
            _serviceName, order.OrderId, order.Items.Count);

        // Simulate inventory check
        // In a real system, this would query a database
        var allInStock = SimulateStockCheck(order.Items);

        await Task.Delay(500); // simulate processing time

        if (allInStock)
        {
            var confirmed = new InventoryConfirmed
            {
                OrderId = order.OrderId,
                CorrelationId = order.CorrelationId,
                ConfirmedAt = DateTime.UtcNow
            };

            await PublishAsync(confirmed, "inventory.confirmed");

            Log.Information(
                "[{Service}] Inventory CONFIRMED for Order {OrderId}",
                _serviceName, order.OrderId);
        }
        else
        {
            var failed = new InventoryFailed
            {
                OrderId = order.OrderId,
                CorrelationId = order.CorrelationId,
                Reason = "One or more items are out of stock"
            };

            await PublishAsync(failed, "inventory.failed");

            Log.Warning(
                "[{Service}] Inventory FAILED for Order {OrderId}",
                _serviceName, order.OrderId);
        }
    }

    private bool SimulateStockCheck(List<OrderItemDto> items)
    {
        // Check if any item has quantity > stock limit
        foreach (var item in items)
        {
            // Stadium has stock of 10, everything else 20
            var maxStock = item.ProductId == 5 ? 10 : 20;
            if (item.Quantity > maxStock)
            {
                Log.Warning(
                    "[{Service}] Product {ProductId} requested {Qty} " +
                    "but only {Stock} available",
                    _serviceName, item.ProductId, item.Quantity, maxStock);
                return false;
            }
        }
        return true;
    }

    private async Task PublishAsync<T>(T message, string queueName)
    {
        if (_channel == null) return;

        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        await _channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: queueName,
            mandatory: false,
            basicProperties: new BasicProperties { Persistent = true },
            body: body);

        Log.Information(
            "[{Service}] Published {MessageType} to {Queue}",
            _serviceName, typeof(T).Name, queueName);
    }

    private async Task ConnectWithRetryAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _configuration["RabbitMQ:Host"] ?? "localhost",
            Port = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672"),
            UserName = _configuration["RabbitMQ:Username"] ?? "guest",
            Password = _configuration["RabbitMQ:Password"] ?? "guest"
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