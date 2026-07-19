using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetPlatform.Application.DTOs;
using PetPlatform.Application.Interfaces;

namespace PetPlatform.Host.MVC.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "Permission:Categories.Manage")]
public class CategoryController : Controller
{
    private readonly ICategoryService _categoryService;

    public CategoryController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Categories";

        var categories = await _categoryService.GetTreeAsync();
        return View(categories);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        ViewData["Title"] = id.HasValue ? "Edit Category" : "Create Category";

        ViewBag.ParentCategories = await _categoryService.GetAllAsync();

        if (id.HasValue)
        {
            var category = await _categoryService.GetByIdAsync(id.Value);
            if (category is null) return NotFound();

            return View(new CreateCategoryDto
            {
                Name = category.Name,
                ParentId = category.ParentId
            });
        }

        return View(new CreateCategoryDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CreateCategoryDto dto)
    {
        if (!ModelState.IsValid)
        {
            ViewData["Title"] = id == 0 ? "Create Category" : "Edit Category";
            ViewBag.ParentCategories = await _categoryService.GetAllAsync();
            return View(dto);
        }

        var result = id == 0
            ? await _categoryService.CreateAsync(dto)
            : await _categoryService.UpdateAsync(id, dto);

        if (result.IsSuccess)
        {
            TempData["Success"] = id == 0
                ? "Category created successfully."
                : "Category updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError(string.Empty, result.Error ?? "An error occurred.");
        ViewData["Title"] = id == 0 ? "Create Category" : "Edit Category";
        ViewBag.ParentCategories = await _categoryService.GetAllAsync();
        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _categoryService.DeleteAsync(id);
        if (result.IsSuccess)
        {
            TempData["Success"] = "Category deleted successfully.";
        }
        else
        {
            TempData["Error"] = result.Error;
        }

        return RedirectToAction(nameof(Index));
    }
}
