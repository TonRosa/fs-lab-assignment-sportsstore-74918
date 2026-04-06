using Serilog;
using Payment.Service;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/payment-.txt", rollingInterval: RollingInterval.Day)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("ServiceName", "Payment.Service")
    .CreateLogger();

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<PaymentWorker>();
builder.Logging.ClearProviders();
builder.Logging.AddSerilog();

var host = builder.Build();
Log.Information("[Payment.Service] Starting...");
host.Run();