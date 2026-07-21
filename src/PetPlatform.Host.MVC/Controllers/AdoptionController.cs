using Microsoft.AspNetCore.Mvc;
using PetPlatform.Application.DTOs;
using PetPlatform.Application.Interfaces;
using PetPlatform.Domain.Enums;

namespace PetPlatform.Host.MVC.Controllers;

public class AdoptionController : Controller
{
    private readonly IAdoptionService _adoptionService;

    public AdoptionController(IAdoptionService adoptionService)
    {
        _adoptionService = adoptionService;
    }

    public async Task<IActionResult> Index(
        PetSpecies? species,
        string? breed,
        int? minAge,
        int? maxAge,
        string? location,
        string? search,
        int page = 1)
    {
        var filter = new AdoptionListingFilterDto
        {
            Species = species,
            Breed = breed,
            MinAge = minAge,
            MaxAge = maxAge,
            Location = location,
            SearchTerm = search,
            Page = page,
            PageSize = 12
        };

        var result = await _adoptionService.GetActiveListingsAsync(filter);

        ViewData["Species"] = species;
        ViewData["Breed"] = breed;
        ViewData["MinAge"] = minAge;
        ViewData["MaxAge"] = maxAge;
        ViewData["Location"] = location;
        ViewData["Search"] = search;

        return View(result);
    }

    public async Task<IActionResult> Details(int id)
    {
        var listing = await _adoptionService.GetListingByIdAsync(id);
        if (listing is null) return NotFound();
        return View(listing);
    }
}
