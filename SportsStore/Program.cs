using Microsoft.EntityFrameworkCore;
using SportsStore.Models;
using SportsStore.Services;
using Stripe;
using Serilog;

// Serilog configuration - mantida igual
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()

    // Reduce ASP.NET noise
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)

    // Structured context enrichment
    .Enrich.FromLogContext()
    //.Enrich.WithMachineName()
    //.Enrich.WithThreadId()

    // Console logging (minimal)
    .WriteTo.Console(
        restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Warning)

    // File logging (full detail)
    .WriteTo.File(
        "logs/sportsstore.txt",
        rollingInterval: RollingInterval.Day,
        restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Debug)

    // Seq centralized logging
    .WriteTo.Seq(
        "http://localhost:5341",
        restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information)

    .CreateLogger();

try
{
    Log.Information("Iniciando SportsStore");

    var builder = WebApplication.CreateBuilder(args);

    // Add Serilog ao builder (DEVE vir antes de outros serviços)
    builder.Host.UseSerilog();

    // Stripe configuration - com segurança
    var stripeSecretKey = builder.Configuration["Stripe:SecretKey"];
    if (!string.IsNullOrEmpty(stripeSecretKey))
    {
        StripeConfiguration.ApiKey = stripeSecretKey;
        Log.Information("Stripe configurado com sucesso");
    }
    else
    {
        Log.Warning("Chave secreta do Stripe não encontrada");
    }

    // Registrar serviços
    builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("Stripe"));
    builder.Services.AddScoped<IPaymentService, StripePaymentService>();

    builder.Services.AddControllersWithViews();
    builder.Services.AddRazorPages();
    builder.Services.AddServerSideBlazor();

    builder.Services.AddDbContext<StoreDbContext>(opts => {
        opts.UseSqlServer(
            builder.Configuration["ConnectionStrings:SportsStoreConnection"]);
    });

    builder.Services.AddScoped<IStoreRepository, EFStoreRepository>();
    builder.Services.AddScoped<IOrderRepository, EFOrderRepository>();

    builder.Services.AddDistributedMemoryCache();
    builder.Services.AddSession();
    builder.Services.AddScoped<Cart>(sp => SessionCart.GetCart(sp));
    builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

    var app = builder.Build();

    app.UseStaticFiles();
    app.UseSession();

    // Rotas
    app.MapControllerRoute("catpage",
        "{category}/Page{productPage:int}",
        new { Controller = "Home", action = "Index" });

    app.MapControllerRoute("page", "Page{productPage:int}",
        new { Controller = "Home", action = "Index", productPage = 1 });

    app.MapControllerRoute("category", "{category}",
        new { Controller = "Home", action = "Index", productPage = 1 });

    app.MapControllerRoute("pagination",
        "Products/Page{productPage}",
        new { Controller = "Home", action = "Index", productPage = 1 });

    app.MapDefaultControllerRoute();
    app.MapRazorPages();
    app.MapBlazorHub();
    app.MapFallbackToPage("/admin/{*catchall}", "/Admin/Index");

    // Seed data
    SeedData.EnsurePopulated(app);

    Log.Information("SportsStore iniciado com sucesso");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Falha ao iniciar SportsStore");
}
finally
{
    Log.CloseAndFlush();
}