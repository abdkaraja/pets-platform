using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PetPlatform.Application.DTOs;
using PetPlatform.Application.Interfaces;
using PetPlatform.Infrastructure.Identity;

namespace PetPlatform.Host.MVC.Areas.Customer.Controllers;

[Area("Customer")]
[Authorize]
public class MyPetsController : Controller
{
    private readonly IPetService _petService;
    private readonly IFileStorageService _fileStorageService;
    private readonly UserManager<ApplicationUser> _userManager;

    public MyPetsController(
        IPetService petService,
        IFileStorageService fileStorageService,
        UserManager<ApplicationUser> userManager)
    {
        _petService = petService;
        _fileStorageService = fileStorageService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId)) return Challenge();

        var pets = await _petService.GetAllByOwnerAsync(userId);
        return View(pets);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreatePetDto dto)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId)) return Challenge();

        if (dto.Photo is { Length: > 0 })
        {
            try
            {
                dto.PhotoPath = await _fileStorageService.SaveFileAsync(dto.Photo, "pets");
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("Photo", ex.Message);
                return View(dto);
            }
        }

        var result = await _petService.CreateAsync(dto, userId);
        if (result.IsSuccess)
        {
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError(string.Empty, result.Error ?? "Failed to create pet.");
        return View(dto);
    }

    public async Task<IActionResult> Details(int id)
    {
        var pet = await _petService.GetByIdAsync(id);
        if (pet is null) return NotFound();
        return View(pet);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId)) return Challenge();

        var pet = await _petService.GetByIdAsync(id);
        if (pet is null) return NotFound();
        if (pet.OwnerId != userId) return Forbid();

        var dto = new UpdatePetDto
        {
            Name = pet.Name,
            Breed = pet.Breed,
            Age = pet.Age,
            Weight = pet.Weight
        };
        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UpdatePetDto dto)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId)) return Challenge();

        var result = await _petService.UpdateAsync(id, dto, userId);
        if (result.IsSuccess)
        {
            return RedirectToAction(nameof(Details), new { id });
        }

        ModelState.AddModelError(string.Empty, result.Error ?? "Failed to update pet.");
        return View(dto);
    }

    public async Task<IActionResult> Delete(int id)
    {
        var pet = await _petService.GetByIdAsync(id);
        if (pet is null) return NotFound();
        return View(pet);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId)) return Challenge();

        var result = await _petService.DeleteAsync(id, userId);
        if (result.IsSuccess)
        {
            return RedirectToAction(nameof(Index));
        }

        return RedirectToAction(nameof(Index));
    }
}
