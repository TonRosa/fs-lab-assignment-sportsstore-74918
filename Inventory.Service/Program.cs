using Serilog;
using Inventory.Service;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/inventory-.txt", rollingInterval: RollingInterval.Day)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("ServiceName", "Inventory.Service")
    .CreateLogger();

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<InventoryWorker>();
builder.Logging.ClearProviders();
builder.Logging.AddSerilog();

var host = builder.Build();
Log.Information("[Inventory.Service] Starting...");
host.Run();