using Microsoft.AspNetCore.Mvc;
using SportsStore.Models;

namespace SportsStore.Controllers
{

    public class OrderController : Controller
    {
        //private IOrderRepository repository;
        //private Cart cart;

        //public OrderController(IOrderRepository repoService, Cart cartService) {
        //            repository = repoService;
        //            cart = cartService;
        //        }

        //        public ViewResult Checkout() => View(new Order());

        //        [HttpPost]
        //        public IActionResult Checkout(Order order) {
        //            if (cart.Lines.Count() == 0) {
        //                ModelState.AddModelError("", "Sorry, your cart is empty!");
        //                return View();
        //            }
        //            if (ModelState.IsValid) {
        //                order.Lines = cart.Lines.ToArray();
        //                repository.SaveOrder(order);

        //                cart.Clear();
        //                return RedirectToPage("/Completed", new { orderId = order.OrderID });
        //            } else {
        //                return View();
        //            }

        //        }

        //    }
        //}


        private readonly IOrderRepository _repository;
        private readonly Cart _cart;
        private readonly IPaymentService _paymentService;
        private readonly ILogger<OrderController> _logger;

        public OrderController(
            IOrderRepository repoService,
            Cart cartService,
            IPaymentService paymentService,
            ILogger<OrderController> logger)
        {
            _repository = repoService;
            _cart = cartService;
            _paymentService = paymentService;
            _logger = logger;
        }

        public ViewResult Checkout() => View(new Order());

        [HttpPost]
        public async Task<IActionResult> Checkout(Order order, string stripeToken)
        {
            if (_cart.Lines.Count == 0)
            {
                ModelState.AddModelError("", "Sorry, your cart is empty!");
                _logger.LogWarning("Checkout attempted with empty cart");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Process payment
                    var paymentRequest = new PaymentRequest
                    {
                        OrderId = order.OrderID,
                        Amount = _cart.ComputeTotalValue(),
                        CustomerEmail = order.Name ?? "customer@example.com",
                        PaymentMethodId = stripeToken,
                        Metadata = new Dictionary<string, string>
                        {
                            ["ShippingAddress"] = $"{order.Line1}, {order.City}, {order.State}",
                            ["GiftWrap"] = order.GiftWrap.ToString()
                        }
                    };

                    var paymentResult = await _paymentService.ProcessPayment(paymentRequest);

                    if (paymentResult.Success)
                    {
                        // Save order
                        order.Lines = _cart.Lines.ToArray();
                        order.PaymentIntentId = paymentResult.TransactionId;
                        order.PaymentStatus = paymentResult.Status.ToString();

                        _repository.SaveOrder(order);

                        _logger.LogInformation("Order {OrderId} placed successfully", order.OrderID);

                        _cart.Clear();

                        return RedirectToPage("/Completed", new { orderId = order.OrderID });
                    }
                    else
                    {
                        ModelState.AddModelError("", $"Payment failed: {paymentResult.ErrorMessage}");
                        return View(order);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing order");
                    ModelState.AddModelError("", "An error occurred while processing your order.");
                    return View(order);
                }
            }

            return View(order);
        }
      
    }
}