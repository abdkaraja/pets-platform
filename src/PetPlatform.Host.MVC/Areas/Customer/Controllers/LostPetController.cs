using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetPlatform.Application.DTOs;
using PetPlatform.Application.Interfaces;

namespace PetPlatform.Host.MVC.Areas.Customer.Controllers;

[Area("Customer")]
[Authorize]
public class LostPetController : Controller
{
    private readonly ILostPetService _lostPetService;

    public LostPetController(ILostPetService lostPetService)
    {
        _lostPetService = lostPetService;
    }

    private string? GetUserId() =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    public async Task<IActionResult> MyReports()
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Challenge();

        var reports = await _lostPetService.GetMyReportsAsync(userId);
        return View(reports);
    }

    [HttpGet]
    public IActionResult Create()
    {
        var dto = new CreateLostPetReportDto { DateReported = DateTime.Today };
        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateLostPetReportDto dto, List<IFormFile> photos)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Challenge();

        var validationError = ValidatePhotos(photos);
        if (validationError != null) return BadRequest(validationError);

        var result = await _lostPetService.CreateReportAsync(dto, userId, photos);
        if (result.IsSuccess)
        {
            TempData["Success"] = "Your lost pet report has been submitted! We're checking for matches...";
            return RedirectToAction(nameof(ReportDetails), new { id = result.Value.Id });
        }

        ModelState.AddModelError(string.Empty, result.Error ?? "Failed to create report.");
        return View(dto);
    }

    public async Task<IActionResult> ReportDetails(int id)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Challenge();

        var report = await _lostPetService.GetReportByIdAsync(id);
        if (report is null) return NotFound();

        ViewBag.IsOwner = report.ReporterUserId == userId;
        return View(report);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Challenge();

        var report = await _lostPetService.GetReportByIdAsync(id);
        if (report is null) return NotFound();

        if (report.ReporterUserId != userId) return Forbid();
        if (report.Status != Domain.Enums.LostPetReportStatus.Open)
        {
            TempData["Error"] = "You can only edit open reports.";
            return RedirectToAction(nameof(ReportDetails), new { id });
        }

        var dto = new UpdateLostPetReportDto
        {
            Color = report.Color,
            Breed = report.Breed,
            Location = report.Location,
            DateReported = report.DateReported,
            Description = report.Description
        };

        ViewBag.ExistingPhotos = report.Photos;
        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UpdateLostPetReportDto dto, List<IFormFile> photos)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Challenge();

        var validationError = ValidatePhotos(photos);
        if (validationError != null) return BadRequest(validationError);

        var result = await _lostPetService.UpdateReportAsync(id, dto, userId, photos);
        if (result.IsSuccess)
        {
            TempData["Success"] = "Report updated successfully.";
            return RedirectToAction(nameof(ReportDetails), new { id });
        }

        ModelState.AddModelError(string.Empty, result.Error ?? "Failed to update report.");
        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Resolve(int id)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Challenge();

        var result = await _lostPetService.ResolveReportAsync(id, userId);
        if (result.IsSuccess)
        {
            TempData["Success"] = "Report marked as resolved.";
        }
        else
        {
            TempData["Error"] = result.Error ?? "Failed to resolve report.";
        }

        return RedirectToAction(nameof(ReportDetails), new { id });
    }

    public async Task<IActionResult> Notifications()
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Challenge();

        var notifications = await _lostPetService.GetMyNotificationsAsync(userId);
        return View(notifications);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkNotificationRead(int notificationId)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId)) return Challenge();

        var result = await _lostPetService.MarkNotificationReadAsync(notificationId, userId);
        if (result.IsSuccess)
        {
            TempData["Success"] = "Notification marked as read.";
        }
        else
        {
            TempData["Error"] = result.Error ?? "Failed to mark notification as read.";
        }

        return RedirectToAction(nameof(Notifications));
    }

    private static string? ValidatePhotos(List<IFormFile> photos)
    {
        if (photos == null || photos.Count == 0) return null;

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        var maxSizeBytes = 5 * 1024 * 1024; // 5 MB
        var maxCount = 5;

        foreach (var file in photos)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(ext))
                return $"File type '{ext}' is not allowed. Allowed types: {string.Join(", ", allowedExtensions)}.";
            if (file.Length > maxSizeBytes)
                return $"File '{file.FileName}' exceeds the {maxSizeBytes / 1024 / 1024}MB limit.";
        }

        if (photos.Count > maxCount)
            return $"Maximum {maxCount} photos allowed.";

        return null;
    }
}
