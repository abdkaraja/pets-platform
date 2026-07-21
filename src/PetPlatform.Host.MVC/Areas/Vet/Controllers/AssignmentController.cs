using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PetPlatform.Application.DTOs;
using PetPlatform.Application.Interfaces;
using PetPlatform.Infrastructure.Identity;

namespace PetPlatform.Host.MVC.Areas.Vet.Controllers;

[Area("Vet")]
[Authorize(Roles = "Vet")]
public class AssignmentController : Controller
{
    private readonly IVetService _vetService;
    private readonly UserManager<ApplicationUser> _userManager;

    public AssignmentController(
        IVetService vetService,
        UserManager<ApplicationUser> userManager)
    {
        _vetService = vetService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Pending()
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId)) return Challenge();

        var pendingRequests = await _vetService.GetPendingRequestsAsync(userId);
        ViewData["Title"] = "Pending Requests";
        return View(pendingRequests);
    }

    public async Task<IActionResult> Details(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId)) return Challenge();

        var assignment = await _vetService.GetAssignmentByIdAsync(id, userId);

        if (assignment == null)
        {
            return NotFound();
        }

        ViewData["Title"] = "Assignment Details";
        return View(assignment);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Accept(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId)) return Challenge();

        var result = await _vetService.AcceptAssignmentAsync(id, userId);
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error ?? "Failed to accept assignment.";
        }

        return RedirectToAction(nameof(Pending));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string? reason)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId)) return Challenge();

        var result = await _vetService.RejectAssignmentAsync(id, userId, reason);
        if (!result.IsSuccess)
        {
            TempData["Error"] = result.Error ?? "Failed to reject assignment.";
        }

        return RedirectToAction(nameof(Pending));
    }
}
