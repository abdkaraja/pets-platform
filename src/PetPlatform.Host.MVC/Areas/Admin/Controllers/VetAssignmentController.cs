using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetPlatform.Application.DTOs;
using PetPlatform.Application.Interfaces;
using PetPlatform.Domain.Enums;
using PetPlatform.Infrastructure.Identity;

namespace PetPlatform.Host.MVC.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "Permission:Users.View")]
public class VetAssignmentController : Controller
{
    private readonly IVetService _vetService;
    private readonly IApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public VetAssignmentController(
        IVetService vetService,
        IApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _vetService = vetService;
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(int page = 1, VetAssignmentStatus? status = null)
    {
        ViewData["Title"] = "Vet Assignments";
        ViewData["StatusFilter"] = status;

        const int pageSize = 20;

        var query = _context.VetAssignments
            .Include(va => va.Pet)
            .Include(va => va.VetProfile)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(va => va.Status == status.Value);

        var totalCount = await query.CountAsync();
        var assignments = await query
            .OrderByDescending(va => va.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(va => new VetAssignmentDto
            {
                Id = va.Id,
                PetId = va.PetId,
                VetProfileId = va.VetProfileId,
                RequestedByUserId = va.RequestedByUserId,
                Status = va.Status,
                RejectionReason = va.RejectionReason,
                CreatedAt = va.CreatedAt,
                PetName = va.Pet != null ? va.Pet.Name : "",
                VetFullName = va.VetProfile != null ? va.VetProfile.FullName : "",
                VetClinic = va.VetProfile != null ? va.VetProfile.Clinic : null
            })
            .ToListAsync();

        ViewBag.CurrentPage = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        return View(assignments);
    }

    [Authorize(Policy = "Permission:Users.Manage")]
    public async Task<IActionResult> Create()
    {
        ViewData["Title"] = "Create Vet Assignment";

        // Populate dropdowns
        var pets = await _context.Pets.OrderBy(p => p.Name).ToListAsync();
        var vetProfiles = await _context.VetProfiles
            .Where(vp => vp.IsApproved)
            .OrderBy(vp => vp.FullName)
            .ToListAsync();

        ViewBag.Pets = pets;
        ViewBag.VetProfiles = vetProfiles;

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "Permission:Users.Manage")]
    public async Task<IActionResult> Create(int petId, int vetProfileId)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId)) return Challenge();

        // Create the assignment request
        var result = await _vetService.RequestAssignmentAsync(petId, vetProfileId, userId);
        if (result.IsSuccess && result.Value != null)
        {
            // Auto-accept since admin is creating directly
            var acceptResult = await _vetService.AcceptAssignmentAsync(result.Value.Id, userId);
            if (acceptResult.IsSuccess)
            {
                TempData["Success"] = "Vet assignment created successfully.";
                return RedirectToAction(nameof(Index));
            }
            TempData["Error"] = acceptResult.Error ?? "Assignment created but could not be auto-accepted.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Error"] = result.Error ?? "Failed to create assignment.";

        // Reload dropdowns
        var pets = await _context.Pets.OrderBy(p => p.Name).ToListAsync();
        var vetProfiles = await _context.VetProfiles
            .Where(vp => vp.IsApproved)
            .OrderBy(vp => vp.FullName)
            .ToListAsync();

        ViewBag.Pets = pets;
        ViewBag.VetProfiles = vetProfiles;

        return View();
    }
}
