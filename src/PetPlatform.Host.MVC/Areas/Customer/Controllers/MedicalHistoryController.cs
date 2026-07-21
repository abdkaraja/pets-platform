using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PetPlatform.Application.DTOs;
using PetPlatform.Application.Interfaces;
using PetPlatform.Infrastructure.Identity;

namespace PetPlatform.Host.MVC.Areas.Customer.Controllers;

[Area("Customer")]
[Authorize]
public class MedicalHistoryController : Controller
{
    private readonly IMedicalRecordService _medicalRecordService;
    private readonly IApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public MedicalHistoryController(
        IMedicalRecordService medicalRecordService,
        IApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _medicalRecordService = medicalRecordService;
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(int petId)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId)) return Challenge();

        // Verify pet ownership
        var pet = await _context.Pets.FindAsync(petId);
        if (pet is null) return NotFound();
        if (pet.OwnerId != userId) return Forbid();

        var records = await _medicalRecordService.GetMedicalHistoryAsync(petId);
        ViewData["PetName"] = pet.Name;

        return View(records);
    }
}
