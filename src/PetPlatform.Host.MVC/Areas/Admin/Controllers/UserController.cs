using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetPlatform.Infrastructure.Identity;

namespace PetPlatform.Host.MVC.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "Permission:Users.View")]
public class UserController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;

    public UserController(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<IActionResult> Index(int page = 1, int pageSize = 20)
    {
        ViewData["Title"] = "User Management";

        var totalUsers = await _userManager.Users.CountAsync();
        var users = await _userManager.Users
            .OrderBy(u => u.Email)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var userViewModels = new List<UserListItemViewModel>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var isLockedOut = await _userManager.IsLockedOutAsync(user);

            userViewModels.Add(new UserListItemViewModel
            {
                Id = user.Id,
                Email = user.Email ?? "",
                Roles = string.Join(", ", roles),
                IsLockedOut = isLockedOut,
                CreatedAt = user.CreatedAt
            });
        }

        ViewBag.CurrentPage = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalPages = (int)Math.Ceiling((double)totalUsers / pageSize);

        return View(userViewModels);
    }

    public async Task<IActionResult> Details(string id)
    {
        if (string.IsNullOrEmpty(id)) return NotFound();

        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        var isLockedOut = await _userManager.IsLockedOutAsync(user);
        var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);

        var viewModel = new UserDetailViewModel
        {
            Id = user.Id,
            Email = user.Email ?? "",
            UserName = user.UserName ?? "",
            Roles = roles.ToList(),
            IsLockedOut = isLockedOut,
            LockoutEnd = lockoutEnd,
            CreatedAt = user.CreatedAt
        };

        ViewData["Title"] = $"User: {user.Email}";
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "Permission:Users.Manage")]
    public async Task<IActionResult> Activate(string id)
    {
        if (string.IsNullOrEmpty(id)) return NotFound();

        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        await _userManager.SetLockoutEndDateAsync(user, null);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "Permission:Users.Manage")]
    public async Task<IActionResult> Deactivate(string id)
    {
        if (string.IsNullOrEmpty(id)) return NotFound();

        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        // Permanent lockout — DateTimeOffset.MaxValue
        await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Policy = "Permission:Users.Manage")]
    public async Task<IActionResult> AssignRole(string id)
    {
        if (string.IsNullOrEmpty(id)) return NotFound();

        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var currentRoles = await _userManager.GetRolesAsync(user);
        var allRoles = await _roleManager.Roles.Select(r => r.Name!).ToListAsync();

        var viewModel = new AssignRoleViewModel
        {
            UserId = user.Id,
            UserEmail = user.Email ?? "",
            CurrentRoles = currentRoles.ToList(),
            AvailableRoles = allRoles.Except(currentRoles).ToList()
        };

        ViewData["Title"] = $"Assign Role: {user.Email}";
        return View("Edit", viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "Permission:Users.Manage")]
    public async Task<IActionResult> AssignRole(string id, string role)
    {
        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(role)) return NotFound();

        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        if (!await _roleManager.RoleExistsAsync(role))
            return BadRequest("Role does not exist.");

        var result = await _userManager.AddToRoleAsync(user, role);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
        }

        return RedirectToAction(nameof(AssignRole), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "Permission:Users.Manage")]
    public async Task<IActionResult> RemoveRole(string id, string role)
    {
        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(role)) return NotFound();

        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var result = await _userManager.RemoveFromRoleAsync(user, role);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
        }

        return RedirectToAction(nameof(AssignRole), new { id });
    }
}

// ── View Models ────────────────────────────────────────────────────────

public class UserListItemViewModel
{
    public string Id { get; set; } = "";
    public string Email { get; set; } = "";
    public string Roles { get; set; } = "";
    public bool IsLockedOut { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UserDetailViewModel
{
    public string Id { get; set; } = "";
    public string Email { get; set; } = "";
    public string UserName { get; set; } = "";
    public List<string> Roles { get; set; } = new();
    public bool IsLockedOut { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AssignRoleViewModel
{
    public string UserId { get; set; } = "";
    public string UserEmail { get; set; } = "";
    public List<string> CurrentRoles { get; set; } = new();
    public List<string> AvailableRoles { get; set; } = new();
}
