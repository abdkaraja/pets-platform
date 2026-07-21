using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetPlatform.Application.DTOs;
using PetPlatform.Application.Interfaces;
using PetPlatform.Infrastructure.Identity;

namespace PetPlatform.Host.MVC.Areas.Customer.Controllers;

[Area("Customer")]
[Authorize]
public class VetDiscoveryController : Controller
{
    private readonly IVetService _vetService;
    private readonly IApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public VetDiscoveryController(
        IVetService vetService,
        IApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _vetService = vetService;
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(string? search, string? specialty, bool? isAvailable, int page = 1)
    {
        var filter = new VetSearchFilterDto
        {
            SearchTerm = search,
            Specialty = specialty,
            IsAvailable = isAvailable,
            Page = page
        };

        var results = await _vetService.SearchVetsAsync(filter);

        // Preserve filter state in ViewData for the view
        ViewData["SearchTerm"] = search;
        ViewData["Specialty"] = specialty;
        ViewData["IsAvailable"] = isAvailable;
        ViewData["CurrentPage"] = page;

        return View(results);
    }

    public async Task<IActionResult> Details(int id)
    {
        var profile = await _context.VetProfiles.FindAsync(id);
        if (profile is null || !profile.IsApproved)
            return NotFound();

        var dto = new VetProfileDto
        {
            Id = profile.Id,
            UserId = profile.UserId,
            FullName = profile.FullName,
            Clinic = profile.Clinic,
            Specialty = profile.Specialty,
            Bio = profile.Bio,
            ServicesOffered = profile.ServicesOffered,
            IsAvailable = profile.IsAvailable,
            IsApproved = profile.IsApproved,
            CreatedAt = profile.CreatedAt
        };

        // Get availability schedule
        var availability = await _context.VetAvailability
            .Where(va => va.VetProfileId == id)
            .OrderBy(va => va.DayOfWeek)
            .ToListAsync();

        ViewBag.Availability = availability;

        // Get user's pets for assignment request dropdown
        var userId = _userManager.GetUserId(User);
        if (!string.IsNullOrEmpty(userId))
        {
            var userPets = await _context.Pets.Where(p => p.OwnerId == userId).ToListAsync();
            ViewBag.UserPets = userPets;
        }

        return View(dto);
    }

    public async Task<IActionResult> RequestAssignment(int vetProfileId)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId)) return Challenge();

        // Verify vet exists and is approved
        var vetProfile = await _context.VetProfiles.FindAsync(vetProfileId);
        if (vetProfile is null || !vetProfile.IsApproved) return NotFound();

        // Get user's pets for the selection dropdown
        var userPets = await _context.Pets.Where(p => p.OwnerId == userId).ToListAsync();
        ViewBag.UserPets = userPets;
        ViewBag.VetProfile = vetProfile;

        return View(new RequestAssignmentDto { VetProfileId = vetProfileId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestAssignment(RequestAssignmentDto dto)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId)) return Challenge();

        var result = await _vetService.RequestAssignmentAsync(dto.PetId, dto.VetProfileId, userId);
        if (result.IsSuccess)
        {
            TempData["Success"] = "Assignment request sent successfully. Waiting for vet approval.";
            return RedirectToAction("Details", new { id = dto.VetProfileId });
        }

        TempData["Error"] = result.Error ?? "Failed to send assignment request.";

        // Reload for GET view
        var userPets = await _context.Pets.Where(p => p.OwnerId == userId).ToListAsync();
        var vetProfile = await _context.VetProfiles.FindAsync(dto.VetProfileId);
        ViewBag.UserPets = userPets;
        ViewBag.VetProfile = vetProfile;

        return View(dto);
    }
}
