using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetPlatform.Application.DTOs;
using PetPlatform.Application.Interfaces;

namespace PetPlatform.Host.MVC.Areas.Customer.Controllers;

[Area("Customer")]
[Authorize]
public class MyAccountController : Controller
{
    private readonly ICustomerService _customerService;

    public MyAccountController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Challenge();

        var profile = await _customerService.GetProfileAsync(userId);
        return View(profile);
    }

    public async Task<IActionResult> Edit()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Challenge();

        var profile = await _customerService.GetProfileAsync(userId);
        var dto = new UpdateCustomerProfileDto
        {
            FullName = profile.FullName,
            Address = profile.Address,
            Phone = profile.Phone,
            City = profile.City,
            NotificationPreferences = profile.NotificationPreferences
        };
        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UpdateCustomerProfileDto dto)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Challenge();

        var result = await _customerService.UpdateProfileAsync(userId, dto);
        if (result.IsSuccess)
        {
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError(string.Empty, result.Error ?? "Failed to update profile.");
        return View(dto);
    }
}
