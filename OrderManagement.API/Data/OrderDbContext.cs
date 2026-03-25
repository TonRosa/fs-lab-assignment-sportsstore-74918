using Microsoft.EntityFrameworkCore;
using OrderManagement.API.Models;

namespace OrderManagement.API.Data;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options)
        : base(options) { }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<InventoryRecord> InventoryRecords => Set<InventoryRecord>();
    public DbSet<PaymentRecord> PaymentRecords => Set<PaymentRecord>();
    public DbSet<ShipmentRecord> ShipmentRecords => Set<ShipmentRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Order
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(o => o.Id);
            entity.Property(o => o.TotalAmount)
                  .HasColumnType("decimal(18,2)");
            entity.Property(o => o.Status)
                  .HasConversion<string>();
            entity.HasOne(o => o.Customer)
                  .WithMany(c => c.Orders)
                  .HasForeignKey(o => o.CustomerId);
            entity.HasOne(o => o.PaymentRecord)
                  .WithOne(p => p.Order)
                  .HasForeignKey<PaymentRecord>(p => p.OrderId);
            entity.HasOne(o => o.ShipmentRecord)
                  .WithOne(s => s.Order)
                  .HasForeignKey<ShipmentRecord>(s => s.OrderId);
            entity.HasOne(o => o.InventoryRecord)
                  .WithOne(i => i.Order)
                  .HasForeignKey<InventoryRecord>(i => i.OrderId);
        });

        // OrderItem
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(oi => oi.Id);
            entity.Property(oi => oi.UnitPrice)
                  .HasColumnType("decimal(18,2)");
            entity.HasOne(oi => oi.Order)
                  .WithMany(o => o.Items)
                  .HasForeignKey(oi => oi.OrderId);
            entity.Ignore(oi => oi.TotalPrice);
        });

        // Product
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Price)
                  .HasColumnType("decimal(18,2)");
        });

        // Seed some products
        modelBuilder.Entity<Product>().HasData(
            new Product { Id = 1, Name = "Kayak", Description = "A boat for one person", Price = 275, Category = "Watersports", Stock = 100 },
            new Product { Id = 2, Name = "Lifejacket", Description = "Protective and fashionable", Price = 48.95m, Category = "Watersports", Stock = 100 },
            new Product { Id = 3, Name = "Soccer Ball", Description = "FIFA-approved size and weight", Price = 19.50m, Category = "Soccer", Stock = 100 },
            new Product { Id = 4, Name = "Corner Flags", Description = "Give your pitch a professional touch", Price = 34.95m, Category = "Soccer", Stock = 100 },
            new Product { Id = 5, Name = "Stadium", Description = "Flat-packed 35,000-seat stadium", Price = 79500m, Category = "Soccer", Stock = 10 }
        );
    }
}