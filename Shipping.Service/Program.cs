using Serilog;
using Shipping.Service;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/shipping-.txt", rollingInterval: RollingInterval.Day)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("ServiceName", "Shipping.Service")
    .CreateLogger();

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<ShippingWorker>();
builder.Logging.ClearProviders();
builder.Logging.AddSerilog();

var host = builder.Build();
Log.Information("[Shipping.Service] Starting...");
host.Run();