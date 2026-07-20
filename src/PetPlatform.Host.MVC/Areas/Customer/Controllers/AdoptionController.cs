using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetPlatform.Application.DTOs;
using PetPlatform.Application.Interfaces;

namespace PetPlatform.Host.MVC.Areas.Customer.Controllers;

[Area("Customer")]
[Authorize]
public class AdoptionController : Controller
{
    private readonly IAdoptionService _adoptionService;

    public AdoptionController(IAdoptionService adoptionService)
    {
        _adoptionService = adoptionService;
    }

    private string? GetUserId() =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    public async Task<IActionResult> MyApplications()
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Challenge();

        var applications = await _adoptionService.GetMyApplicationsAsync(userId);
        return View(applications);
    }

    [HttpGet]
    public async Task<IActionResult> Apply(int listingId)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Challenge();

        var listing = await _adoptionService.GetListingByIdAsync(listingId);
        if (listing is null) return NotFound();

        ViewBag.Listing = listing;

        var dto = new CreateApplicationDto { ListingId = listingId };
        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Apply(CreateApplicationDto dto)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Challenge();

        var listing = await _adoptionService.GetListingByIdAsync(dto.ListingId);
        if (listing is null) return NotFound();

        ViewBag.Listing = listing;

        var result = await _adoptionService.SubmitApplicationAsync(dto, userId);
        if (result.IsSuccess)
        {
            TempData["Success"] = "Your application has been submitted!";
            return RedirectToAction(nameof(ApplicationDetails), new { id = result.Value.Id });
        }

        ModelState.AddModelError(string.Empty, result.Error ?? "Failed to submit application.");
        return View(dto);
    }

    public async Task<IActionResult> ApplicationDetails(int id)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Challenge();

        var applications = await _adoptionService.GetMyApplicationsAsync(userId);
        var application = applications.FirstOrDefault(a => a.Id == id);
        if (application is null) return NotFound();

        return View(application);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Withdraw(int id)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Challenge();

        var result = await _adoptionService.WithdrawApplicationAsync(id, userId);
        if (result.IsSuccess)
        {
            TempData["Success"] = "Application withdrawn.";
        }
        else
        {
            TempData["Error"] = result.Error ?? "Failed to withdraw application.";
        }

        return RedirectToAction(nameof(MyApplications));
    }
}
