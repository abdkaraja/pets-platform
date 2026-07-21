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
public class VetManagementController : Controller
{
    private readonly IVetService _vetService;
    private readonly IMedicalRecordService _medicalRecordService;
    private readonly IApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public VetManagementController(
        IVetService vetService,
        IMedicalRecordService medicalRecordService,
        IApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _vetService = vetService;
        _medicalRecordService = medicalRecordService;
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(int page = 1, bool? isApproved = null)
    {
        ViewData["Title"] = "Vet Management";
        ViewData["IsApproved"] = isApproved;

        var results = await _vetService.GetAllVetProfilesAsync(page, 20, isApproved);
        return View(results);
    }

    public async Task<IActionResult> Details(int id)
    {
        var profiles = await _vetService.GetAllVetProfilesAsync(1, 1000, null);
        var vetProfile = profiles.Items.FirstOrDefault(vp => vp.Id == id);
        if (vetProfile is null) return NotFound();

        ViewData["Title"] = $"Vet: {vetProfile.FullName}";
        return View(vetProfile);
    }

    public async Task<IActionResult> PendingApprovals()
    {
        ViewData["Title"] = "Pending Vet Approvals";

        var results = await _vetService.GetAllVetProfilesAsync(1, 100, false);
        return View(results.Items);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "Permission:Users.Manage")]
    public async Task<IActionResult> Approve(int id)
    {
        var result = await _vetService.ApproveVetAsync(id);
        if (result.IsSuccess)
        {
            TempData["Success"] = "Vet profile approved successfully.";
        }
        else
        {
            TempData["Error"] = result.Error ?? "Failed to approve vet profile.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "Permission:Users.Manage")]
    public async Task<IActionResult> Reject(int id)
    {
        var result = await _vetService.RejectVetAsync(id);
        if (result.IsSuccess)
        {
            TempData["Success"] = "Vet profile rejected.";
        }
        else
        {
            TempData["Error"] = result.Error ?? "Failed to reject vet profile.";
        }
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> MedicalRecords(int? petId = null, int page = 1)
    {
        ViewData["Title"] = "All Medical Records";

        const int pageSize = 20;

        // Combine all three record types into a unified view
        var vaccinations = await _context.VaccinationRecords
            .Include(v => v.Pet)
            .OrderByDescending(v => v.DateAdministered)
            .ToListAsync();

        var medications = await _context.MedicationRecords
            .Include(m => m.Pet)
            .OrderByDescending(m => m.StartDate)
            .ToListAsync();

        var visitNotes = await _context.VetVisitNotes
            .Include(v => v.Pet)
            .OrderByDescending(v => v.VisitDate)
            .ToListAsync();

        // Map to summary DTOs
        var allRecords = new List<MedicalRecordSummaryDto>();

        allRecords.AddRange(vaccinations.Select(v => new MedicalRecordSummaryDto
        {
            Id = v.Id,
            RecordType = MedicalRecordType.Vaccination,
            PetId = v.PetId,
            PetName = v.Pet?.Name ?? "",
            VetUserName = v.VetUserId,
            Date = v.DateAdministered,
            Summary = v.VaccineName
        }));

        allRecords.AddRange(medications.Select(m => new MedicalRecordSummaryDto
        {
            Id = m.Id,
            RecordType = MedicalRecordType.Medication,
            PetId = m.PetId,
            PetName = m.Pet?.Name ?? "",
            VetUserName = m.VetUserId,
            Date = m.StartDate,
            Summary = $"{m.MedicationName} - {m.Dosage} ({m.Frequency})"
        }));

        allRecords.AddRange(visitNotes.Select(v => new MedicalRecordSummaryDto
        {
            Id = v.Id,
            RecordType = MedicalRecordType.VisitNote,
            PetId = v.PetId,
            PetName = v.Pet?.Name ?? "",
            VetUserName = v.VetUserId,
            Date = v.VisitDate,
            Summary = v.Assessment
        }));

        // Filter by petId if provided
        if (petId.HasValue)
        {
            allRecords = allRecords.Where(r => r.PetId == petId.Value).ToList();
        }

        // Sort by date descending
        allRecords = allRecords.OrderByDescending(r => r.Date).ToList();

        var totalCount = allRecords.Count;
        var pagedRecords = allRecords
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        ViewBag.CurrentPage = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        ViewBag.PetId = petId;

        return View(pagedRecords);
    }
}
