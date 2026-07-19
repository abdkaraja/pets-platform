using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetPlatform.Application.DTOs;
using PetPlatform.Application.Interfaces;
using PetPlatform.Domain.Enums;

namespace PetPlatform.Host.MVC.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "Permission:Orders.Manage")]
public class OrderManagementController : Controller
{
    private readonly IOrderService _orderService;

    public OrderManagementController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    public async Task<IActionResult> Index(OrderStatus? status, string? searchTerm)
    {
        ViewData["Title"] = "Orders";

        var orders = await _orderService.GetAllOrdersAsync(status, searchTerm);
        ViewBag.StatusFilter = status;
        ViewBag.SearchTerm = searchTerm;

        return View(orders);
    }

    public async Task<IActionResult> Details(int id)
    {
        var order = await _orderService.GetOrderByIdAsync(id);
        if (order is null) return NotFound();

        ViewData["Title"] = $"Order #{order.Id}";

        var adminOrder = new AdminOrderDto
        {
            Id = order.Id,
            CustomerEmail = order.CustomerEmail,
            TotalAmount = order.TotalAmount,
            Status = order.Status,
            CreatedAt = order.CreatedAt,
            ItemCount = order.Items.Count,
            NextStatus = GetNextAllowedStatus(order.Status)
        };

        ViewBag.OrderItems = order.Items;
        ViewBag.StatusHistory = order.StatusHistory;
        ViewBag.ShippingAddress = order.ShippingAddress;

        return View(adminOrder);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, OrderStatus newStatus)
    {
        var result = await _orderService.UpdateOrderStatusAsync(id, newStatus);

        if (result.IsSuccess)
        {
            TempData["Success"] = $"Order status updated to {newStatus}.";
        }
        else
        {
            TempData["Error"] = result.Error;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    private static OrderStatus? GetNextAllowedStatus(OrderStatus current)
    {
        return current switch
        {
            OrderStatus.Pending => OrderStatus.Processing,
            OrderStatus.Processing => OrderStatus.Shipped,
            OrderStatus.Shipped => OrderStatus.Delivered,
            OrderStatus.Delivered => null,
            _ => null
        };
    }
}
