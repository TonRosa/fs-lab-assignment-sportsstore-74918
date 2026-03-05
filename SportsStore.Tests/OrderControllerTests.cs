using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using SportsStore.Controllers;
using SportsStore.Models;
using SportsStore.Services;
using System.Threading.Tasks;
using Xunit;

namespace SportsStore.Tests;

public class OrderControllerTests
{
    [Fact]
    public void Checkout_ReturnsView_WithValidModel()
    {
        // Arrange
        var mockRepo = new Mock<IOrderRepository>();
        var cart = new Cart();
        var mockPaymentService = new Mock<IPaymentService>();  // NOVO
        var mockLogger = new Mock<ILogger<OrderController>>(); // NOVO

        var controller = new OrderController(
            mockRepo.Object,
            cart,
            mockPaymentService.Object,  // ADICIONAR
            mockLogger.Object            // ADICIONAR
        );

        // Act
        var result = controller.Checkout();

        // Assert
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task Checkout_Post_ReturnsView_WhenModelInvalid()
    {
        // Arrange
        var mockRepo = new Mock<IOrderRepository>();
        var cart = new Cart();
        var mockPaymentService = new Mock<IPaymentService>();  // NOVO
        var mockLogger = new Mock<ILogger<OrderController>>(); // NOVO

        var controller = new OrderController(
            mockRepo.Object,
            cart,
            mockPaymentService.Object,  // ADICIONAR
            mockLogger.Object            // ADICIONAR
        );

        controller.ModelState.AddModelError("error", "test error");

        var order = new Order();

        // Act
        var result = await controller.Checkout(order, "test_token"); // NOVO: stripeToken

        // Assert
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task Checkout_Post_ReturnsRedirect_WhenValid()
    {
        // Arrange
        var mockRepo = new Mock<IOrderRepository>();
        var cart = new Cart();
        cart.AddItem(new Product { ProductID = 1, Name = "Test", Price = 10 }, 1);

        var mockPaymentService = new Mock<IPaymentService>();  // NOVO
        mockPaymentService.Setup(x => x.ProcessPayment(It.IsAny<PaymentRequest>()))
            .ReturnsAsync(new PaymentResult { Success = true, TransactionId = "test_123" });

        var mockLogger = new Mock<ILogger<OrderController>>(); // NOVO

        var controller = new OrderController(
            mockRepo.Object,
            cart,
            mockPaymentService.Object,  // ADICIONAR
            mockLogger.Object            // ADICIONAR
        );

        var order = new Order();

        // Act
        var result = await controller.Checkout(order, "test_token"); // NOVO: stripeToken

        // Assert
        Assert.IsType<RedirectToPageResult>(result);
        mockRepo.Verify(x => x.SaveOrder(It.IsAny<Order>()), Times.Once);
    }
}