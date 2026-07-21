using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetPlatform.Application.DTOs;
using PetPlatform.Application.Interfaces;

namespace PetPlatform.Host.MVC.Areas.ServiceProvider.Controllers;

[Area("ServiceProvider")]
[Authorize(Policy = "Permission:Adoptions.Manage")]
public class AdoptionController : Controller
{
    private readonly IAdoptionService _adoptionService;

    public AdoptionController(IAdoptionService adoptionService)
    {
        _adoptionService = adoptionService;
    }

    private string? GetUserId() =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    public async Task<IActionResult> Listings()
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Challenge();

        var listings = await _adoptionService.GetShelterListingsAsync(userId);
        return View(listings);
    }

    [HttpGet]
    public IActionResult CreateListing()
    {
        return View(new CreateListingDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateListing(CreateListingDto dto)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Challenge();

        var result = await _adoptionService.CreateListingAsync(dto, userId);
        if (result.IsSuccess)
        {
            TempData["Success"] = "Listing created.";
            return RedirectToAction(nameof(Listings));
        }

        ModelState.AddModelError(string.Empty, result.Error ?? "Failed to create listing.");
        return View(dto);
    }

    [HttpGet]
    public async Task<IActionResult> EditListing(int id)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Challenge();

        var listing = await _adoptionService.GetListingByIdAsync(id);
        if (listing is null || listing.ShelterUserId != userId) return NotFound();

        var dto = new UpdateListingDto
        {
            Title = listing.Title,
            Description = listing.Description,
            Location = listing.Location
        };

        ViewBag.PetName = listing.PetName;
        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditListing(int id, UpdateListingDto dto)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Challenge();

        var result = await _adoptionService.UpdateListingAsync(id, dto, userId);
        if (result.IsSuccess)
        {
            TempData["Success"] = "Listing updated.";
            return RedirectToAction(nameof(Listings));
        }

        ModelState.AddModelError(string.Empty, result.Error ?? "Failed to update listing.");
        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CloseListing(int id)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Challenge();

        var result = await _adoptionService.CloseListingAsync(id, userId);
        if (result.IsSuccess)
        {
            TempData["Success"] = "Listing closed.";
        }
        else
        {
            TempData["Error"] = result.Error ?? "Failed to close listing.";
        }

        return RedirectToAction(nameof(Listings));
    }

    public async Task<IActionResult> Applications(int listingId)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Challenge();

        var applications = await _adoptionService.GetApplicationsForListingAsync(listingId, userId);
        ViewBag.ListingId = listingId;
        return View(applications);
    }

    [HttpGet]
    public async Task<IActionResult> ReviewApplication(int applicationId, int listingId)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Challenge();

        var application = await _adoptionService.GetApplicationForReviewAsync(applicationId, userId);
        if (application is null) return NotFound();

        ViewBag.Application = application;
        return View(new ReviewApplicationDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReviewApplication(int applicationId, ReviewApplicationDto dto, int listingId)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Challenge();

        var result = await _adoptionService.ReviewApplicationAsync(applicationId, dto, userId);
        if (result.IsSuccess)
        {
            TempData["Success"] = $"Application {dto.Status}.";
            return RedirectToAction(nameof(Applications), new { listingId });
        }

        var application = await _adoptionService.GetApplicationForReviewAsync(applicationId, userId);
        ViewBag.Application = application;

        ModelState.AddModelError(string.Empty, result.Error ?? "Failed to review application.");
        return View(dto);
    }
}
