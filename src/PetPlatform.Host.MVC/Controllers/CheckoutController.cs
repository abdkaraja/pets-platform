using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetPlatform.Application.DTOs;
using PetPlatform.Application.Interfaces;

namespace PetPlatform.Host.MVC.Controllers;

[Authorize]
public class CheckoutController : Controller
{
    private readonly IPaymentService _paymentService;
    private readonly ICartService _cartService;
    private readonly IConfiguration _configuration;

    public CheckoutController(
        IPaymentService paymentService,
        ICartService cartService,
        IConfiguration configuration)
    {
        _paymentService = paymentService;
        _cartService = cartService;
        _configuration = configuration;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public async Task<IActionResult> Index()
    {
        var cart = await _cartService.GetCartAsync(UserId);
        if (cart is null || !cart.Items.Any())
            return RedirectToAction("Index", "Cart");

        var dto = new CheckoutDto
        {
            CartItems = cart.Items,
            Subtotal = cart.Subtotal,
            ShippingCost = 5.99m
        };

        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSession([FromBody] CheckoutDto dto)
    {
        var cart = await _cartService.GetCartAsync(UserId);
        if (cart is null || !cart.Items.Any())
            return BadRequest(new { success = false, message = "Cart is empty" });

        var successUrl = Url.Action("Confirmation", "Checkout",
            new { session_id = "{CHECKOUT_SESSION_ID}" }, Request.Scheme)!;
        var cancelUrl = Url.Action("Index", "Checkout", null, Request.Scheme)!;

        var result = await _paymentService.CreateCheckoutSessionAsync(UserId, successUrl, cancelUrl);
        if (!result.IsSuccess)
            return BadRequest(new { success = false, message = result.Error });

        return Json(new
        {
            sessionId = result.Value!.SessionId,
            clientSecret = result.Value.ClientSecret,
            publishableKey = _configuration["Stripe:PublishableKey"]
        });
    }

    public async Task<IActionResult> Confirmation(string session_id)
    {
        var order = await _paymentService.GetOrderAfterPaymentAsync(session_id);
        if (order is null)
        {
            // Webhook may not have processed yet
            ViewData["SessionId"] = session_id;
            ViewData["WaitingForConfirmation"] = true;
            return View("Confirmation");
        }

        return View(order);
    }
}
