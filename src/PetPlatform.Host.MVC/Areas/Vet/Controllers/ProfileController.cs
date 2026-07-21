using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PetPlatform.Application.DTOs;
using PetPlatform.Application.Interfaces;
using PetPlatform.Infrastructure.Identity;

namespace PetPlatform.Host.MVC.Areas.Vet.Controllers;

[Area("Vet")]
[Authorize(Roles = "Vet")]
public class ProfileController : Controller
{
    private readonly IVetService _vetService;
    private readonly UserManager<ApplicationUser> _userManager;

    public ProfileController(
        IVetService vetService,
        UserManager<ApplicationUser> userManager)
    {
        _vetService = vetService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId)) return Challenge();

        var profile = await _vetService.GetProfileByUserIdAsync(userId);
        if (profile == null)
        {
            return RedirectToAction(nameof(Create));
        }

        ViewData["Title"] = "My Profile";
        return View(profile);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId)) return Challenge();

        var user = await _userManager.FindByIdAsync(userId);
        var dto = new CreateVetProfileDto
        {
            UserId = userId,
            FullName = user?.UserName ?? ""
        };

        ViewData["Title"] = "Create Profile";
        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateVetProfileDto dto)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId)) return Challenge();

        dto.UserId = userId;

        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Create Profile";
            return View(dto);
        }

        var result = await _vetService.CreateProfileAsync(dto, userId);
        if (result.IsSuccess)
        {
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError(string.Empty, result.Error ?? "Failed to create profile.");
        ViewData["Title"] = "Create Profile";
        return View(dto);
    }

    [HttpGet]
    public async Task<IActionResult> Edit()
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId)) return Challenge();

        var profile = await _vetService.GetProfileByUserIdAsync(userId);
        if (profile == null)
        {
            return RedirectToAction(nameof(Create));
        }

        if (profile.UserId != userId)
        {
            return NotFound();
        }

        var dto = new UpdateVetProfileDto
        {
            FullName = profile.FullName,
            Clinic = profile.Clinic,
            Specialty = profile.Specialty,
            Bio = profile.Bio,
            ServicesOffered = profile.ServicesOffered
        };

        ViewData["Title"] = "Edit Profile";
        ViewBag.ProfileId = profile.Id;
        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UpdateVetProfileDto dto)
    {
        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(userId)) return Challenge();

        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Edit Profile";
            ViewBag.ProfileId = id;
            return View(dto);
        }

        var result = await _vetService.UpdateProfileAsync(id, dto, userId);
        if (result.IsSuccess)
        {
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError(string.Empty, result.Error ?? "Failed to update profile.");
        ViewData["Title"] = "Edit Profile";
        ViewBag.ProfileId = id;
        return View(dto);
    }
}
