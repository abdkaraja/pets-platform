using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PetPlatform.Application.DTOs;
using PetPlatform.Application.Interfaces;
using PetPlatform.Infrastructure.Identity;

namespace PetPlatform.Host.MVC.Areas.Vet.Controllers;

[Area("Vet")]
[Authorize(Roles = "Vet")]
public class PetsController : Controller
{
    private readonly IVetService _vetService;
    private readonly IMedicalRecordService _medicalRecordService;
    private readonly UserManager<ApplicationUser> _userManager;

    public PetsController(
        IVetService vetService,
        IMedicalRecordService medicalRecordService,
        UserManager<ApplicationUser> userManager)
    {
        _vetService = vetService;
        _medicalRecordService = medicalRecordService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId)) return Challenge();

        var assignments = await _vetService.GetAcceptedAssignmentsAsync(userId);
        ViewData["Title"] = "Assigned Pets";
        return View(assignments);
    }

    public async Task<IActionResult> Details(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId)) return Challenge();

        var assignment = await _vetService.GetActiveAssignmentAsync(id, userId);
        if (assignment == null)
        {
            return Forbid();
        }

        var recentRecords = (await _medicalRecordService.GetRecentRecordsAsync(id, 5)).ToList();
        var vaccinations = (await _medicalRecordService.GetVaccinationsByPetIdAsync(id)).ToList();
        var medications = (await _medicalRecordService.GetMedicationsByPetIdAsync(id)).ToList();
        var visitNotes = (await _medicalRecordService.GetVisitNotesByPetIdAsync(id)).ToList();

        var model = new PetDetailsViewModel
        {
            PetId = id,
            PetName = assignment.PetName,
            Assignment = assignment,
            RecentRecords = recentRecords,
            Vaccinations = vaccinations,
            Medications = medications,
            VisitNotes = visitNotes
        };

        ViewData["Title"] = $"Pet: {assignment.PetName}";
        return View(model);
    }
}

public class PetDetailsViewModel
{
    public int PetId { get; set; }
    public string PetName { get; set; } = string.Empty;
    public VetAssignmentDto Assignment { get; set; } = new();
    public List<MedicalRecordSummaryDto> RecentRecords { get; set; } = new();
    public List<VaccinationRecordDto> Vaccinations { get; set; } = new();
    public List<MedicationRecordDto> Medications { get; set; } = new();
    public List<VetVisitNoteDto> VisitNotes { get; set; } = new();
}
