using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Moq;
using OrderManagement.API.CQRS.Commands;
using OrderManagement.API.CQRS.Queries;
using OrderManagement.API.Data;
using OrderManagement.API.DTOs;
using OrderManagement.API.Mapping;
using OrderManagement.API.Messaging;
using OrderManagement.API.Models;
using Shared.Contracts.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SportsStore.Tests;

public class OrderManagementTests
{
    private OrderDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new OrderDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    private IMapper CreateMapper()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<MappingProfile>());
        return config.CreateMapper();
    }

    // ─── Order Status Tests ───────────────────────────────

    [Fact]
    public async Task UpdateOrderStatus_ChangesStatusCorrectly()
    {
        // Arrange
        var db = CreateInMemoryDb();
        var customer = new Customer { Name = "Test", Email = "test@test.com" };
        db.Customers.Add(customer);
        var order = new Order
        {
            CustomerId = customer.Id,
            Status = OrderStatus.Submitted,
            ShippingName = "Test",
            ShippingLine1 = "123 St",
            ShippingCity = "Dublin",
            ShippingState = "Leinster",
            ShippingCountry = "Ireland"
        };
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var handler = new UpdateOrderStatusCommandHandler(db);

        // Act
        var result = await handler.Handle(
            new UpdateOrderStatusCommand(order.Id, OrderStatus.Completed),
            CancellationToken.None);

        // Assert
        Assert.True(result);
        var updated = await db.Orders.FindAsync(order.Id);
        Assert.Equal(OrderStatus.Completed, updated!.Status);
    }

    [Fact]
    public async Task UpdateOrderStatus_ReturnsFalse_WhenOrderNotFound()
    {
        // Arrange
        var db = CreateInMemoryDb();
        var handler = new UpdateOrderStatusCommandHandler(db);

        // Act
        var result = await handler.Handle(
            new UpdateOrderStatusCommand(Guid.NewGuid(), OrderStatus.Completed),
            CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    // ─── Query Tests ──────────────────────────────────────

    [Fact]
    public async Task GetOrderById_ReturnsCorrectOrder()
    {
        // Arrange
        var db = CreateInMemoryDb();
        var mapper = CreateMapper();
        var customer = new Customer { Name = "John", Email = "john@test.com" };
        db.Customers.Add(customer);
        var order = new Order
        {
            CustomerId = customer.Id,
            Status = OrderStatus.Submitted,
            ShippingName = "John",
            ShippingLine1 = "123 St",
            ShippingCity = "Dublin",
            ShippingState = "Leinster",
            ShippingCountry = "Ireland"
        };
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var handler = new GetOrderByIdQueryHandler(db, mapper);

        // Act
        var result = await handler.Handle(
            new GetOrderByIdQuery(order.Id),
            CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(order.Id, result!.Id);
    }

    [Fact]
    public async Task GetOrderById_ReturnsNull_WhenNotFound()
    {
        // Arrange
        var db = CreateInMemoryDb();
        var mapper = CreateMapper();
        var handler = new GetOrderByIdQueryHandler(db, mapper);

        // Act
        var result = await handler.Handle(
            new GetOrderByIdQuery(Guid.NewGuid()),
            CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetOrders_ReturnsAllOrders()
    {
        // Arrange
        var db = CreateInMemoryDb();
        var mapper = CreateMapper();
        var customer = new Customer { Name = "John", Email = "john@test.com" };
        db.Customers.Add(customer);
        db.Orders.AddRange(
            new Order
            {
                CustomerId = customer.Id,
                Status = OrderStatus.Submitted,
                ShippingName = "John",
                ShippingLine1 = "123 St",
                ShippingCity = "Dublin",
                ShippingState = "Leinster",
                ShippingCountry = "Ireland"
            },
            new Order
            {
                CustomerId = customer.Id,
                Status = OrderStatus.Completed,
                ShippingName = "John",
                ShippingLine1 = "123 St",
                ShippingCity = "Dublin",
                ShippingState = "Leinster",
                ShippingCountry = "Ireland"
            }
        );
        await db.SaveChangesAsync();

        var handler = new GetOrdersQueryHandler(db, mapper);

        // Act
        var result = await handler.Handle(
            new GetOrdersQuery(),
            CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetOrders_FiltersByStatus()
    {
        // Arrange
        var db = CreateInMemoryDb();
        var mapper = CreateMapper();
        var customer = new Customer { Name = "John", Email = "john@test.com" };
        db.Customers.Add(customer);
        db.Orders.AddRange(
            new Order
            {
                CustomerId = customer.Id,
                Status = OrderStatus.Submitted,
                ShippingName = "John",
                ShippingLine1 = "123 St",
                ShippingCity = "Dublin",
                ShippingState = "Leinster",
                ShippingCountry = "Ireland"
            },
            new Order
            {
                CustomerId = customer.Id,
                Status = OrderStatus.Completed,
                ShippingName = "John",
                ShippingLine1 = "123 St",
                ShippingCity = "Dublin",
                ShippingState = "Leinster",
                ShippingCountry = "Ireland"
            }
        );
        await db.SaveChangesAsync();

        var handler = new GetOrdersQueryHandler(db, mapper);

        // Act
        var result = await handler.Handle(
            new GetOrdersQuery(OrderStatus.Completed),
            CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal(OrderStatus.Completed, result[0].Status);
    }

    // ─── Customer Orders Tests ────────────────────────────

    [Fact]
    public async Task GetCustomerOrders_ReturnsOnlyCustomerOrders()
    {
        // Arrange
        var db = CreateInMemoryDb();
        var mapper = CreateMapper();

        var customer1 = new Customer { Name = "John", Email = "john@test.com" };
        var customer2 = new Customer { Name = "Jane", Email = "jane@test.com" };
        db.Customers.AddRange(customer1, customer2);

        db.Orders.AddRange(
            new Order
            {
                CustomerId = customer1.Id,
                Status = OrderStatus.Submitted,
                ShippingName = "John",
                ShippingLine1 = "123 St",
                ShippingCity = "Dublin",
                ShippingState = "Leinster",
                ShippingCountry = "Ireland"
            },
            new Order
            {
                CustomerId = customer2.Id,
                Status = OrderStatus.Completed,
                ShippingName = "Jane",
                ShippingLine1 = "456 St",
                ShippingCity = "Cork",
                ShippingState = "Munster",
                ShippingCountry = "Ireland"
            }
        );
        await db.SaveChangesAsync();

        var handler = new GetCustomerOrdersQueryHandler(db, mapper);

        // Act
        var result = await handler.Handle(
            new GetCustomerOrdersQuery(customer1.Id),
            CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal(customer1.Id, result[0].CustomerId);
    }

    // ─── Order Status Transition Tests ───────────────────

    [Fact]
    public async Task Order_StatusTransitions_AreValid()
    {
        // Arrange
        var db = CreateInMemoryDb();
        var customer = new Customer { Name = "Test", Email = "test@test.com" };
        db.Customers.Add(customer);
        var order = new Order
        {
            CustomerId = customer.Id,
            Status = OrderStatus.Submitted,
            ShippingName = "Test",
            ShippingLine1 = "123 St",
            ShippingCity = "Dublin",
            ShippingState = "Leinster",
            ShippingCountry = "Ireland"
        };
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var handler = new UpdateOrderStatusCommandHandler(db);

        // Act - simulate full order lifecycle
        await handler.Handle(
            new UpdateOrderStatusCommand(order.Id,
                OrderStatus.InventoryConfirmed), CancellationToken.None);
        await handler.Handle(
            new UpdateOrderStatusCommand(order.Id,
                OrderStatus.PaymentApproved), CancellationToken.None);
        await handler.Handle(
            new UpdateOrderStatusCommand(order.Id,
                OrderStatus.Completed), CancellationToken.None);

        // Assert
        var finalOrder = await db.Orders.FindAsync(order.Id);
        Assert.Equal(OrderStatus.Completed, finalOrder!.Status);
    }
}