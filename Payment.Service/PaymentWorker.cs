using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Contracts.Events;
using Serilog;

namespace Payment.Service;

public class PaymentWorker : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly string _serviceName = "Payment.Service";
    private IConnection? _connection;
    private IChannel? _channel;

    public PaymentWorker(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Log.Information("[{Service}] Starting...", _serviceName);

        await ConnectWithRetryAsync(stoppingToken);

        if (_channel == null) return;

        // Declare all queues
        await _channel.QueueDeclareAsync(
            queue: "inventory.confirmed",
            durable: true, exclusive: false, autoDelete: false);

        await _channel.QueueDeclareAsync(
            queue: "payment.approved",
            durable: true, exclusive: false, autoDelete: false);

        await _channel.QueueDeclareAsync(
            queue: "payment.rejected",
            durable: true, exclusive: false, autoDelete: false);

        await _channel.BasicQosAsync(0, 1, false);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var json = Encoding.UTF8.GetString(body);

            Log.Information(
                "[{Service}] Received message on inventory.confirmed",
                _serviceName);

            try
            {
                var message = JsonSerializer.Deserialize<InventoryConfirmed>(json);
                if (message == null) return;

                await ProcessPaymentAsync(message);
                await _channel.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                Log.Error(ex,
                    "[{Service}] Error processing payment",
                    _serviceName);
                await _channel.BasicNackAsync(ea.DeliveryTag, false, false);
            }
        };

        await _channel.BasicConsumeAsync(
            queue: "inventory.confirmed",
            autoAck: false,
            consumer: consumer);

        Log.Information(
            "[{Service}] Listening on inventory.confirmed queue",
            _serviceName);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    private async Task ProcessPaymentAsync(InventoryConfirmed message)
    {
        Log.Information(
            "[{Service}] Processing payment for Order {OrderId}",
            _serviceName, message.OrderId);

        await Task.Delay(800); // simulate processing time

        var isApproved = SimulatePayment();

        if (isApproved)
        {
            var approved = new PaymentApproved
            {
                OrderId = message.OrderId,
                CorrelationId = message.CorrelationId,
                TransactionId = $"TXN-{Guid.NewGuid().ToString()[..8].ToUpper()}",
                ApprovedAt = DateTime.UtcNow
            };

            await PublishAsync(approved, "payment.approved");

            Log.Information(
                "[{Service}] Payment APPROVED for Order {OrderId}, " +
                "Transaction {TransactionId}",
                _serviceName, message.OrderId, approved.TransactionId);
        }
        else
        {
            var rejected = new PaymentRejected
            {
                OrderId = message.OrderId,
                CorrelationId = message.CorrelationId,
                Reason = "Payment declined by bank"
            };

            await PublishAsync(rejected, "payment.rejected");

            Log.Warning(
                "[{Service}] Payment REJECTED for Order {OrderId}",
                _serviceName, message.OrderId);
        }
    }

    private bool SimulatePayment()
    {
        // 85% approval rate
        var random = new Random();
        return random.NextDouble() > 0.15;
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