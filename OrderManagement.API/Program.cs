using Microsoft.EntityFrameworkCore;
using Serilog;
using OrderManagement.API.Data;
using OrderManagement.API.Messaging;
using OrderManagement.API.Mapping;

// Configure Serilog first
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/orderapi-.txt", rollingInterval: RollingInterval.Day)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("ServiceName", "OrderManagement.API")
    .CreateLogger();

try
{
    Log.Information("[OrderAPI] Starting up...");

    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    // Controllers
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // Database
    builder.Services.AddDbContext<OrderDbContext>(options =>
        options.UseSqlite("Data Source=orders.db"));

    // MediatR - CQRS
    builder.Services.AddMediatR(cfg =>
        cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

    // AutoMapper
    builder.Services.AddAutoMapper(typeof(MappingProfile));

    // RabbitMQ Publisher
    builder.Services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();

    // CORS for Blazor and React frontends
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader());
    });

    var app = builder.Build();

    // Auto-create database on startup
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        db.Database.EnsureCreated();
        Log.Information("[OrderAPI] Database ready");
    }

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseCors("AllowAll");
    app.UseAuthorization();
    app.MapControllers();

    Log.Information("[OrderAPI] Ready to accept requests");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "[OrderAPI] Application failed to start");
}
finally
{
    Log.CloseAndFlush();
}