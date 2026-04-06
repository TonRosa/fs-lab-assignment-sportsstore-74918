using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Contracts.Events;
using Serilog;

namespace Shipping.Service;

public class ShippingWorker : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly string _serviceName = "Shipping.Service";
    private IConnection? _connection;
    private IChannel? _channel;

    public ShippingWorker(IConfiguration configuration)
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
            queue: "payment.approved",
            durable: true, exclusive: false, autoDelete: false);

        await _channel.QueueDeclareAsync(
            queue: "shipping.created",
            durable: true, exclusive: false, autoDelete: false);

        await _channel.QueueDeclareAsync(
            queue: "shipping.failed",
            durable: true, exclusive: false, autoDelete: false);

        await _channel.BasicQosAsync(0, 1, false);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var json = Encoding.UTF8.GetString(body);

            Log.Information(
                "[{Service}] Received message on payment.approved",
                _serviceName);

            try
            {
                var message = JsonSerializer.Deserialize<PaymentApproved>(json);
                if (message == null) return;

                await ProcessShippingAsync(message);
                await _channel.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                Log.Error(ex,
                    "[{Service}] Error processing shipping",
                    _serviceName);
                await _channel.BasicNackAsync(ea.DeliveryTag, false, false);
            }
        };

        await _channel.BasicConsumeAsync(
            queue: "payment.approved",
            autoAck: false,
            consumer: consumer);

        Log.Information(
            "[{Service}] Listening on payment.approved queue",
            _serviceName);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    private async Task ProcessShippingAsync(PaymentApproved message)
    {
        Log.Information(
            "[{Service}] Creating shipment for Order {OrderId}",
            _serviceName, message.OrderId);

        await Task.Delay(600); // simulate processing time

        var success = SimulateShipping();

        if (success)
        {
            var trackingNumber = GenerateTrackingNumber();

            var created = new ShippingCreated
            {
                OrderId = message.OrderId,
                CorrelationId = message.CorrelationId,
                TrackingNumber = trackingNumber,
                EstimatedDispatch = DateTime.UtcNow.AddDays(2)
            };

            await PublishAsync(created, "shipping.created");

            Log.Information(
                "[{Service}] Shipment CREATED for Order {OrderId}, " +
                "Tracking {TrackingNumber}",
                _serviceName, message.OrderId, trackingNumber);
        }
        else
        {
            var failed = new ShippingFailed
            {
                OrderId = message.OrderId,
                CorrelationId = message.CorrelationId,
                Reason = "No courier available for this address"
            };

            await PublishAsync(failed, "shipping.failed");

            Log.Warning(
                "[{Service}] Shipment FAILED for Order {OrderId}",
                _serviceName, message.OrderId);
        }
    }

    private bool SimulateShipping()
    {
        // 95% success rate
        var random = new Random();
        return random.NextDouble() > 0.05;
    }

    private string GenerateTrackingNumber()
    {
        return $"SHIP-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";
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