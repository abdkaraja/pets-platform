using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PetPlatform.Application.DTOs;
using PetPlatform.Application.Interfaces;
using PetPlatform.Infrastructure.Identity;

namespace PetPlatform.Host.MVC.Areas.Vet.Controllers;

[Area("Vet")]
[Authorize(Roles = "Vet")]
public class DashboardController : Controller
{
    private readonly IVetService _vetService;
    private readonly IMedicalRecordService _medicalRecordService;
    private readonly UserManager<ApplicationUser> _userManager;

    public DashboardController(
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

        var profile = await _vetService.GetProfileByUserIdAsync(userId);
        if (profile == null)
        {
            return RedirectToAction("Create", "Profile");
        }

        var acceptedAssignments = (await _vetService.GetAcceptedAssignmentsAsync(userId)).ToList();
        var pendingRequests = (await _vetService.GetPendingRequestsAsync(userId)).ToList();

        var recentRecordsCount = 0;
        foreach (var assignment in acceptedAssignments)
        {
            var records = await _medicalRecordService.GetRecentRecordsAsync(assignment.PetId, 5);
            recentRecordsCount += records.Count();
        }

        var model = new VetDashboardDto
        {
            TotalAssignedPets = acceptedAssignments.Count,
            PendingRequests = pendingRequests.Count,
            RecentRecordsCount = recentRecordsCount,
            AssignedPets = acceptedAssignments,
            PendingRequestsList = pendingRequests
        };

        ViewData["Title"] = "Vet Dashboard";
        return View(model);
    }
}
