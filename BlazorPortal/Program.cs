using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/blazor-.txt", rollingInterval: RollingInterval.Day)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("ServiceName", "BlazorPortal")
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// HTTP client to talk to Order API
builder.Services.AddScoped(sp =>
    new HttpClient
    {
        BaseAddress = new Uri(
            builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5292")
    });

builder.Services.AddScoped<BlazorPortal.Services.OrderApiService>();
builder.Services.AddScoped<BlazorPortal.Services.CartState>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();
app.MapRazorComponents<BlazorPortal.Components.App>()
    .AddInteractiveServerRenderMode();

Log.Information("[BlazorPortal] Starting...");
app.Run();