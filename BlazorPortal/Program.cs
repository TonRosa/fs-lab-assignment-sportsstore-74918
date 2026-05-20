using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using BlazorPortal.Data;

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
// Auth Database
builder.Services.AddDbContext<BlazorPortal.Data.AuthDbContext>(options =>
    options.UseSqlite("Data Source=auth.db"));

// Identity
builder.Services.AddIdentity<BlazorPortal.Data.AppUser,
    Microsoft.AspNetCore.Identity.IdentityRole>(options =>
    {
        options.Password.RequireDigit = false;
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
    })
.AddEntityFrameworkStores<BlazorPortal.Data.AuthDbContext>()
.AddDefaultTokenProviders();

// Auth Service
builder.Services.AddScoped<BlazorPortal.Services.AuthService>();
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

// Create auth database and seed admin
using (var scope = app.Services.CreateScope())
{
    var authDb = scope.ServiceProvider
        .GetRequiredService<BlazorPortal.Data.AuthDbContext>();
    authDb.Database.EnsureCreated();

    var userManager = scope.ServiceProvider
        .GetRequiredService<UserManager<BlazorPortal.Data.AppUser>>();

    // Create admin if not exists
    var admin = await userManager.FindByEmailAsync("admin@sportstore.com");
    if (admin == null)
    {
        var result = await userManager.CreateAsync(new BlazorPortal.Data.AppUser
        {
            UserName = "admin@sportstore.com",
            Email = "admin@sportstore.com",
            FullName = "Admin",
            Role = "Admin"
        }, "Admin123!");

        if (result.Succeeded)
            Log.Information("✅ Admin user created successfully");
        else
            Log.Error("❌ Failed to create admin: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Description)));
    }
    else
    {
        Log.Information("✅ Admin user already exists");
    }
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();
app.MapRazorComponents<BlazorPortal.Components.App>()
    .AddInteractiveServerRenderMode();

Log.Information("[BlazorPortal] Starting...");
app.Run();