using Microsoft.AspNetCore.Mvc;
using PetPlatform.Application.Interfaces;

namespace PetPlatform.Host.MVC.Controllers;

public class PetController : Controller
{
    private readonly IPetService _petService;

    public PetController(IPetService petService)
    {
        _petService = petService;
    }

    public async Task<IActionResult> Index()
    {
        var pets = await _petService.GetAllAsync();
        return View(pets);
    }

    public async Task<IActionResult> Details(int id)
    {
        var pet = await _petService.GetByIdAsync(id);
        if (pet is null) return NotFound();
        return View(pet);
    }
}
