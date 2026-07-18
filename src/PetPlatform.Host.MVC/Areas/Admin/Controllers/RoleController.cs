using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetPlatform.Infrastructure.Identity;

namespace PetPlatform.Host.MVC.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "Permission:Roles.Create")]
public class RoleController : Controller
{
    private readonly RoleManager<ApplicationRole> _roleManager;

    public RoleController(RoleManager<ApplicationRole> roleManager)
    {
        _roleManager = roleManager;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Role Management";

        var roles = await _roleManager.Roles.ToListAsync();
        var roleViewModels = new List<RoleListItemViewModel>();

        foreach (var role in roles)
        {
            var claims = await _roleManager.GetClaimsAsync(role);
            roleViewModels.Add(new RoleListItemViewModel
            {
                Id = role.Id,
                Name = role.Name ?? "",
                Description = role.Description ?? "",
                PermissionCount = claims.Count(c => c.Type == "Permission")
            });
        }

        return View(roleViewModels);
    }

    public IActionResult Create()
    {
        ViewData["Title"] = "Create Role";
        return View(new CreateRoleViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateRoleViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Create Role";
            return View(model);
        }

        if (await _roleManager.RoleExistsAsync(model.Name))
        {
            ModelState.AddModelError(nameof(model.Name), "Role already exists.");
            ViewData["Title"] = "Create Role";
            return View(model);
        }

        var role = new ApplicationRole
        {
            Name = model.Name,
            Description = model.Description
        };

        var result = await _roleManager.CreateAsync(role);
        if (result.Succeeded)
        {
            return RedirectToAction(nameof(Index));
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);

        ViewData["Title"] = "Create Role";
        return View(model);
    }

    public async Task<IActionResult> Details(string id)
    {
        if (string.IsNullOrEmpty(id)) return NotFound();

        var role = await _roleManager.FindByIdAsync(id);
        if (role == null) return NotFound();

        var claims = await _roleManager.GetClaimsAsync(role);
        var permissionClaims = claims.Where(c => c.Type == "Permission").ToList();

        var viewModel = new RoleDetailViewModel
        {
            Id = role.Id,
            Name = role.Name ?? "",
            Description = role.Description ?? "",
            PermissionClaims = permissionClaims.Select(c => new PermissionClaimViewModel
            {
                ClaimId = c.Value,
                ClaimValue = c.Value
            }).ToList()
        };

        ViewData["Title"] = $"Role: {role.Name}";
        return View(viewModel);
    }

    [Authorize(Policy = "Permission:Roles.Create")]
    public async Task<IActionResult> AddClaim(string roleId)
    {
        if (string.IsNullOrEmpty(roleId)) return NotFound();

        var role = await _roleManager.FindByIdAsync(roleId);
        if (role == null) return NotFound();

        var viewModel = new AddClaimViewModel
        {
            RoleId = role.Id,
            RoleName = role.Name ?? ""
        };

        ViewData["Title"] = $"Add Permission: {role.Name}";
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "Permission:Roles.Create")]
    public async Task<IActionResult> AddClaim(string roleId, string claimValue)
    {
        if (string.IsNullOrEmpty(roleId) || string.IsNullOrEmpty(claimValue))
            return NotFound();

        var role = await _roleManager.FindByIdAsync(roleId);
        if (role == null) return NotFound();

        var existingClaims = await _roleManager.GetClaimsAsync(role);
        if (existingClaims.Any(c => c.Type == "Permission" && c.Value == claimValue))
        {
            ModelState.AddModelError(nameof(claimValue), "This permission already exists on this role.");
            var viewModel = new AddClaimViewModel { RoleId = role.Id, RoleName = role.Name ?? "" };
            ViewData["Title"] = $"Add Permission: {role.Name}";
            return View(viewModel);
        }

        var result = await _roleManager.AddClaimAsync(role, new Claim("Permission", claimValue));
        if (result.Succeeded)
        {
            return RedirectToAction(nameof(Details), new { id = roleId });
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);

        var vm = new AddClaimViewModel { RoleId = role.Id, RoleName = role.Name ?? "" };
        ViewData["Title"] = $"Add Permission: {role.Name}";
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "Permission:Roles.Create")]
    public async Task<IActionResult> RemoveClaim(string roleId, string claimId)
    {
        if (string.IsNullOrEmpty(roleId) || string.IsNullOrEmpty(claimId))
            return NotFound();

        var role = await _roleManager.FindByIdAsync(roleId);
        if (role == null) return NotFound();

        var claims = await _roleManager.GetClaimsAsync(role);
        var claim = claims.FirstOrDefault(c => c.Type == "Permission" && c.Value == claimId);
        if (claim == null) return NotFound();

        await _roleManager.RemoveClaimAsync(role, claim);
        return RedirectToAction(nameof(Details), new { id = roleId });
    }
}

// ── View Models ────────────────────────────────────────────────────────

public class RoleListItemViewModel
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public int PermissionCount { get; set; }
}

public class CreateRoleViewModel
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
}

public class RoleDetailViewModel
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public List<PermissionClaimViewModel> PermissionClaims { get; set; } = new();
}

public class PermissionClaimViewModel
{
    public string ClaimId { get; set; } = "";
    public string ClaimValue { get; set; } = "";
}

public class AddClaimViewModel
{
    public string RoleId { get; set; } = "";
    public string RoleName { get; set; } = "";
    public string ClaimValue { get; set; } = "";
}
