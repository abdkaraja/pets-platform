using Microsoft.AspNetCore.Mvc;
using PetPlatform.Application.DTOs;
using PetPlatform.Application.Interfaces;
using PetPlatform.Domain.Enums;

namespace PetPlatform.Host.MVC.Controllers;

public class LostPetController : Controller
{
    private readonly ILostPetService _lostPetService;

    public LostPetController(ILostPetService lostPetService)
    {
        _lostPetService = lostPetService;
    }

    public async Task<IActionResult> Index(
        LostPetReportType? reportType,
        PetSpecies? species,
        string? breed,
        string? color,
        string? location,
        DateTime? dateFrom,
        DateTime? dateTo,
        string? search,
        int page = 1)
    {
        var filter = new LostPetReportFilterDto
        {
            ReportType = reportType,
            Species = species,
            Breed = breed,
            Color = color,
            Location = location,
            DateFrom = dateFrom,
            DateTo = dateTo,
            SearchTerm = search,
            Page = page,
            PageSize = 12
        };

        var result = await _lostPetService.SearchReportsAsync(filter);

        ViewData["ReportType"] = reportType;
        ViewData["Species"] = species;
        ViewData["Breed"] = breed;
        ViewData["Color"] = color;
        ViewData["Location"] = location;
        ViewData["DateFrom"] = dateFrom?.ToString("yyyy-MM-dd");
        ViewData["DateTo"] = dateTo?.ToString("yyyy-MM-dd");
        ViewData["Search"] = search;

        return View(result);
    }

    public async Task<IActionResult> Details(int id)
    {
        var report = await _lostPetService.GetReportByIdAsync(id);
        if (report is null) return NotFound();
        return View(report);
    }
}
