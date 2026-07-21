using Microsoft.AspNetCore.Mvc;
using PetPlatform.Application.DTOs;
using PetPlatform.Application.Interfaces;

namespace PetPlatform.Host.MVC.Controllers;

public class CatalogController : Controller
{
    private readonly IProductService _productService;
    private readonly ICategoryService _categoryService;
    private readonly IBrandService _brandService;

    public CatalogController(
        IProductService productService,
        ICategoryService categoryService,
        IBrandService brandService)
    {
        _productService = productService;
        _categoryService = categoryService;
        _brandService = brandService;
    }

    public async Task<IActionResult> Index(
        string? search,
        int? categoryId,
        string? petType,
        int? brandId,
        decimal? minPrice,
        decimal? maxPrice,
        string? sort,
        int page = 1)
    {
        var filter = new ProductFilterDto
        {
            SearchTerm = search,
            CategoryId = categoryId,
            PetType = petType,
            BrandId = brandId,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            SortBy = sort,
            Page = page,
            PageSize = 12
        };

        var result = await _productService.GetFilteredProductsAsync(filter);
        var categories = await _categoryService.GetAllAsync();
        var brands = await _brandService.GetBrandsWithProductCountsAsync();

        ViewBag.Categories = categories;
        ViewBag.Brands = brands;
        ViewData["Search"] = search;
        ViewData["CategoryId"] = categoryId;
        ViewData["PetType"] = petType;
        ViewData["BrandId"] = brandId;
        ViewData["MinPrice"] = minPrice;
        ViewData["MaxPrice"] = maxPrice;
        ViewData["Sort"] = sort;

        return View(result);
    }

    public async Task<IActionResult> Details(int id)
    {
        var product = await _productService.GetByIdAsync(id);
        if (product is null) return NotFound();
        return View(product);
    }
}
