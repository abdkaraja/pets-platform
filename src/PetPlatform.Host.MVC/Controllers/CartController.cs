using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetPlatform.Application.DTOs;
using PetPlatform.Application.Interfaces;

namespace PetPlatform.Host.MVC.Controllers;

[Authorize]
public class CartController : Controller
{
    private readonly ICartService _cartService;

    public CartController(ICartService cartService)
    {
        _cartService = cartService;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public async Task<IActionResult> Index()
    {
        var cart = await _cartService.GetCartAsync(UserId);
        return View(cart ?? new CartDto { UserId = UserId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddToCart([FromBody] AddToCartDto dto)
    {
        var result = await _cartService.AddToCartAsync(UserId, dto);
        if (!result.IsSuccess)
            return BadRequest(new { success = false, message = result.Error });

        return Json(new { success = true, message = "Added to cart", itemCount = result.Value!.ItemCount });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateQuantity([FromBody] UpdateCartQuantityDto dto)
    {
        var result = await _cartService.UpdateQuantityAsync(UserId, dto);
        if (!result.IsSuccess)
            return BadRequest(new { success = false, message = result.Error });

        return Json(new
        {
            success = true,
            message = "Quantity updated",
            newTotal = result.Value!.Subtotal,
            itemTotal = result.Value.Items.FirstOrDefault(i => i.Id == dto.CartItemId)?.LineTotal
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveItem(int cartItemId)
    {
        var result = await _cartService.RemoveItemAsync(UserId, cartItemId);
        if (!result.IsSuccess)
            return BadRequest(new { success = false, message = result.Error });

        var cart = await _cartService.GetCartAsync(UserId);
        return Json(new { success = true, itemCount = cart?.ItemCount ?? 0 });
    }
}
