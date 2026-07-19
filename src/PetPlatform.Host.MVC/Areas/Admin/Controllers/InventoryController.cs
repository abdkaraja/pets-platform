using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetPlatform.Application.DTOs;
using PetPlatform.Application.Interfaces;

namespace PetPlatform.Host.MVC.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "Permission:Inventory.Manage")]
public class InventoryController : Controller
{
    private readonly IInventoryService _inventoryService;

    public InventoryController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    public async Task<IActionResult> Index(string? searchTerm, string? stockFilter)
    {
        ViewData["Title"] = "Inventory Management";

        var variants = await _inventoryService.GetAllVariantsAsync(searchTerm, stockFilter);
        ViewBag.SearchTerm = searchTerm;
        ViewBag.StockFilter = stockFilter;

        return View(variants);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateStock([FromBody] UpdateStockRequest request)
    {
        var result = await _inventoryService.UpdateStockAsync(request.VariantId, request.NewQuantity);

        if (result.IsSuccess && result.Value is not null)
        {
            var variant = result.Value;
            var status = variant.StockQuantity switch
            {
                0 => "OutOfStock",
                <= 5 => "LowStock",
                _ => "InStock"
            };

            return Json(new
            {
                success = true,
                message = "Stock updated successfully.",
                newStock = variant.StockQuantity,
                status
            });
        }

        return Json(new
        {
            success = false,
            message = result.Error
        });
    }
}

public class UpdateStockRequest
{
    public int VariantId { get; set; }
    public int NewQuantity { get; set; }
}
