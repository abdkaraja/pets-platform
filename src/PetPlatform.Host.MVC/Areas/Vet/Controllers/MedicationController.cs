using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PetPlatform.Application.DTOs;
using PetPlatform.Application.Interfaces;
using PetPlatform.Infrastructure.Identity;

namespace PetPlatform.Host.MVC.Areas.Vet.Controllers;

[Area("Vet")]
[Authorize(Roles = "Vet")]
public class MedicationController : Controller
{
    private readonly IMedicalRecordService _medicalRecordService;
    private readonly IVetService _vetService;
    private readonly UserManager<ApplicationUser> _userManager;

    public MedicationController(
        IMedicalRecordService medicalRecordService,
        IVetService vetService,
        UserManager<ApplicationUser> userManager)
    {
        _medicalRecordService = medicalRecordService;
        _vetService = vetService;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Create(int petId)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId)) return Challenge();

        var assignment = await _vetService.GetActiveAssignmentAsync(petId, userId);
        if (assignment == null)
        {
            return Forbid();
        }

        var dto = new CreateMedicationDto { PetId = petId };
        ViewData["PetName"] = assignment.PetName;
        ViewData["Title"] = $"Add Medication — {assignment.PetName}";
        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateMedicationDto dto)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId)) return Challenge();

        var assignment = await _vetService.GetActiveAssignmentAsync(dto.PetId, userId);
        if (assignment == null)
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            ViewData["PetName"] = assignment.PetName;
            ViewData["Title"] = $"Add Medication — {assignment.PetName}";
            return View(dto);
        }

        var result = await _medicalRecordService.CreateMedicationAsync(dto, userId);
        if (result.IsSuccess)
        {
            return RedirectToAction(nameof(Details), new { id = result.Value!.Id });
        }

        ModelState.AddModelError(string.Empty, result.Error ?? "Failed to create medication record.");
        ViewData["PetName"] = assignment.PetName;
        ViewData["Title"] = $"Add Medication — {assignment.PetName}";
        return View(dto);
    }

    public async Task<IActionResult> Details(int id)
    {
        var record = await _medicalRecordService.GetMedicationByIdAsync(id);
        if (record == null) return NotFound();

        ViewData["Title"] = $"Medication — {record.MedicationName}";
        return View(record);
    }
}
