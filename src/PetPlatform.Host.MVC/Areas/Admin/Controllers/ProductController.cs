using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetPlatform.Application.DTOs;
using PetPlatform.Application.Interfaces;

namespace PetPlatform.Host.MVC.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "Permission:Products.Manage")]
public class ProductController : Controller
{
    private readonly IProductService _productService;
    private readonly ICategoryService _categoryService;
    private readonly IApplicationDbContext _dbContext;

    public ProductController(
        IProductService productService,
        ICategoryService categoryService,
        IApplicationDbContext dbContext)
    {
        _productService = productService;
        _categoryService = categoryService;
        _dbContext = dbContext;
    }

    public async Task<IActionResult> Index(string? searchTerm, int? categoryId, int page = 1)
    {
        ViewData["Title"] = "Products";

        var filter = new ProductFilterDto
        {
            SearchTerm = searchTerm,
            CategoryId = categoryId,
            Page = page,
            PageSize = 10
        };

        var products = await _productService.GetFilteredProductsAsync(filter);
        ViewBag.Categories = await _categoryService.GetAllAsync();
        ViewBag.SearchTerm = searchTerm;
        ViewBag.CategoryId = categoryId;

        return View(products);
    }

    public async Task<IActionResult> Create()
    {
        ViewData["Title"] = "Create Product";

        ViewBag.Categories = await _categoryService.GetAllAsync();
        ViewBag.Brands = _dbContext.Brands
            .Select(b => new BrandDto { Id = b.Id, Name = b.Name })
            .ToList();

        return View(new AdminProductDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdminProductDto dto, IFormFile? imageFile)
    {
        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Create Product";
            ViewBag.Categories = await _categoryService.GetAllAsync();
            ViewBag.Brands = _dbContext.Brands
                .Select(b => new BrandDto { Id = b.Id, Name = b.Name })
                .ToList();
            return View(dto);
        }

        if (imageFile is { Length: > 0 })
        {
            var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "products");
            Directory.CreateDirectory(uploadsDir);

            var uniqueName = $"{Guid.NewGuid()}{Path.GetExtension(imageFile.FileName)}";
            var filePath = Path.Combine(uploadsDir, uniqueName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }

            dto.ImagePath = $"/uploads/products/{uniqueName}";
        }

        var result = await _productService.CreateAsync(dto);
        if (result.IsSuccess)
        {
            TempData["Success"] = "Product created successfully.";
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError(string.Empty, result.Error ?? "An error occurred.");
        ViewData["Title"] = "Create Product";
        ViewBag.Categories = await _categoryService.GetAllAsync();
        ViewBag.Brands = _dbContext.Brands
            .Select(b => new BrandDto { Id = b.Id, Name = b.Name })
            .ToList();
        return View(dto);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var product = await _productService.GetByIdAsync(id);
        if (product is null) return NotFound();

        ViewData["Title"] = "Edit Product";

        var dto = new AdminProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            BasePrice = product.BasePrice,
            CategoryId = product.CategoryId,
            BrandId = product.BrandId,
            PetType = product.PetType,
            ImagePath = product.ImagePath,
            IsActive = product.IsActive
        };

        ViewBag.Categories = await _categoryService.GetAllAsync();
        ViewBag.Brands = _dbContext.Brands
            .Select(b => new BrandDto { Id = b.Id, Name = b.Name })
            .ToList();

        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AdminProductDto dto, IFormFile? imageFile)
    {
        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Edit Product";
            ViewBag.Categories = await _categoryService.GetAllAsync();
            ViewBag.Brands = _dbContext.Brands
                .Select(b => new BrandDto { Id = b.Id, Name = b.Name })
                .ToList();
            return View(dto);
        }

        if (imageFile is { Length: > 0 })
        {
            var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "products");
            Directory.CreateDirectory(uploadsDir);

            var uniqueName = $"{Guid.NewGuid()}{Path.GetExtension(imageFile.FileName)}";
            var filePath = Path.Combine(uploadsDir, uniqueName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }

            dto.ImagePath = $"/uploads/products/{uniqueName}";
        }

        var result = await _productService.UpdateAsync(id, dto);
        if (result.IsSuccess)
        {
            TempData["Success"] = "Product updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError(string.Empty, result.Error ?? "An error occurred.");
        ViewData["Title"] = "Edit Product";
        ViewBag.Categories = await _categoryService.GetAllAsync();
        ViewBag.Brands = _dbContext.Brands
            .Select(b => new BrandDto { Id = b.Id, Name = b.Name })
            .ToList();
        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _productService.DeleteAsync(id);
        if (result.IsSuccess)
        {
            TempData["Success"] = "Product deleted successfully.";
        }
        else
        {
            TempData["Error"] = result.Error;
        }

        return RedirectToAction(nameof(Index));
    }
}
