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
    // Watersports
    new Product { Id = 1, Name = "Kayak", Description = "A boat for one person", Price = 275m, Category = "Watersports", Stock = 30 },
    new Product { Id = 2, Name = "Lifejacket", Description = "Protective and fashionable", Price = 48.95m, Category = "Watersports", Stock = 30 },
    new Product { Id = 3, Name = "Wet Suit", Description = "Full body neoprene wet suit", Price = 129.99m, Category = "Watersports", Stock = 30 },
    new Product { Id = 4, Name = "Snorkel Set", Description = "Mask, snorkel and fins included", Price = 39.99m, Category = "Watersports", Stock = 30 },
    new Product { Id = 5, Name = "Paddle Board", Description = "Inflatable stand-up paddle board", Price = 349.99m, Category = "Watersports", Stock = 15 },

    // Soccer
    new Product { Id = 6, Name = "Soccer Ball", Description = "FIFA-approved size and weight", Price = 19.50m, Category = "Soccer", Stock = 30 },
    new Product { Id = 7, Name = "Corner Flags", Description = "Give your pitch a professional touch", Price = 34.95m, Category = "Soccer", Stock = 30 },
    new Product { Id = 8, Name = "Soccer Boots", Description = "Firm ground soccer boots", Price = 89.99m, Category = "Soccer", Stock = 30 },
    new Product { Id = 9, Name = "Goalkeeper Gloves", Description = "Professional grip goalkeeper gloves", Price = 29.99m, Category = "Soccer", Stock = 30 },
    new Product { Id = 10, Name = "Stadium", Description = "Flat-packed 35,000-seat stadium", Price = 79500m, Category = "Soccer", Stock = 10 },

    // Cycling
    new Product { Id = 11, Name = "Road Bike", Description = "Lightweight carbon road bike", Price = 1299.99m, Category = "Cycling", Stock = 15 },
    new Product { Id = 12, Name = "Mountain Bike", Description = "Full suspension trail bike", Price = 899.99m, Category = "Cycling", Stock = 15 },
    new Product { Id = 13, Name = "Cycling Shoes", Description = "Stiff sole clipless cycling shoes", Price = 89.99m, Category = "Cycling", Stock = 30 },
    new Product { Id = 14, Name = "Cycling Helmet", Description = "Aerodynamic lightweight helmet", Price = 79.99m, Category = "Cycling", Stock = 30 },
    new Product { Id = 15, Name = "Bike Gloves", Description = "Padded cycling gloves", Price = 24.99m, Category = "Cycling", Stock = 30 },

    // Tennis
    new Product { Id = 16, Name = "Tennis Racket", Description = "Professional graphite racket", Price = 159.99m, Category = "Tennis", Stock = 30 },
    new Product { Id = 17, Name = "Tennis Shoes", Description = "Court grip tennis shoes", Price = 79.99m, Category = "Tennis", Stock = 30 },
    new Product { Id = 18, Name = "Tennis Balls", Description = "Pack of 4 pressurised balls", Price = 9.99m, Category = "Tennis", Stock = 50 },
    new Product { Id = 19, Name = "Tennis Bag", Description = "6 racket tennis bag with pockets", Price = 49.99m, Category = "Tennis", Stock = 30 },
    new Product { Id = 20, Name = "Tennis Net", Description = "Full size regulation tennis net", Price = 89.99m, Category = "Tennis", Stock = 15 },

    // American Football
    new Product { Id = 21, Name = "American Football", Description = "Official NFL size football", Price = 29.99m, Category = "American Football", Stock = 30 },
    new Product { Id = 22, Name = "Football Helmet", Description = "Full protection helmet", Price = 189.99m, Category = "American Football", Stock = 20 },
    new Product { Id = 23, Name = "Shoulder Pads", Description = "Impact protection shoulder pads", Price = 129.99m, Category = "American Football", Stock = 20 },
    new Product { Id = 24, Name = "Football Cleats", Description = "High ankle football cleats", Price = 99.99m, Category = "American Football", Stock = 30 },
    new Product { Id = 25, Name = "Football Gloves", Description = "Receiver grip football gloves", Price = 34.99m, Category = "American Football", Stock = 30 },

    // Rugby
    new Product { Id = 26, Name = "Rugby Ball", Description = "Match quality rugby ball", Price = 24.99m, Category = "Rugby", Stock = 30 },
    new Product { Id = 27, Name = "Rugby Boots", Description = "Firm ground rugby boots", Price = 69.99m, Category = "Rugby", Stock = 30 },
    new Product { Id = 28, Name = "Rugby Jersey", Description = "Official match jersey", Price = 59.99m, Category = "Rugby", Stock = 30 },
    new Product { Id = 29, Name = "Rugby Shorts", Description = "Reinforced match shorts", Price = 34.99m, Category = "Rugby", Stock = 30 },
    new Product { Id = 30, Name = "Rugby Scrum Cap", Description = "Protective padded scrum cap", Price = 29.99m, Category = "Rugby", Stock = 30 },

    // Boxing
    new Product { Id = 31, Name = "Boxing Gloves", Description = "Professional leather boxing gloves", Price = 79.99m, Category = "Boxing", Stock = 30 },
    new Product { Id = 32, Name = "Punching Bag", Description = "Heavy duty hanging punch bag", Price = 149.99m, Category = "Boxing", Stock = 15 },
    new Product { Id = 33, Name = "Boxing Helmet", Description = "Full face protection headguard", Price = 89.99m, Category = "Boxing", Stock = 20 },
    new Product { Id = 34, Name = "Hand Wraps", Description = "4.5m cotton hand wraps", Price = 12.99m, Category = "Boxing", Stock = 50 },
    new Product { Id = 35, Name = "Mouthguard", Description = "Custom fit boil and bite mouthguard", Price = 14.99m, Category = "Boxing", Stock = 50 }
);
    }
}