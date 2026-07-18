# Phase 1: Foundation, Identity & Core Profiles - Research

**Researched:** 2026-07-18
**Domain:** ASP.NET Core Identity, Claims-Based Authorization, EF Core Entity Design, File Upload
**Confidence:** HIGH

## Summary

Phase 1 establishes the entire platform foundation: Clean Architecture project scaffolding, ASP.NET Core Identity with custom `ApplicationUser` and `ApplicationRole`, claims-based permission seeding, policy-based authorization, EF Core entities for Pet/CustomerProfile, Areas for Admin/Customer separation, and pet photo upload handling. The research confirms that .NET 10 SDK (10.0.301) is available, ASP.NET Core Identity ships built-in with `IdentityDbContext`, and the established patterns from Microsoft eShopOnWeb and JasonTaylor CleanArchitecture template directly apply.

**Primary recommendation:** Use `IdentityDbContext<ApplicationUser>` in Infrastructure layer, seed 4 roles + permissions via `IHostedService` or startup seeding pattern, scaffold Identity Razor Pages into `Areas/Identity/`, and use Areas for Admin/Customer separation with conventional routing.

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| User registration, login, email verification | Identity Razor Pages (Host.MVC) | Application (services) | Identity RCL handles auth flows; Application layer has no auth logic |
| Role & permission management | Admin Area (Host.MVC) | Infrastructure (Identity seeding) | Admin UI manages roles; seeding is infrastructure startup concern |
| Pet profile CRUD | Application (PetService) | Domain (Pet entity) | Business logic in Application; entity invariants in Domain |
| Customer profile / My Account | Application (CustomerService) | Domain (CustomerProfile entity) | Profile management is application business logic |
| File upload (pet photos) | Infrastructure (FileStorageService) | Host.MVC (Controller) | Storage abstraction in Infrastructure; controller handles HTTP |
| Policy-based authorization | Host.MVC (Program.cs) | Infrastructure (SeedData) | Policy registration in composition root; claim seeding in Infrastructure |
| EF Core data access | Infrastructure (DbContext) | Domain (interfaces) | DbContext implements Domain interfaces; migrations live in Infrastructure |

## Standard Stack

### Core (Phase 1)

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Microsoft.AspNetCore.Identity.EntityFrameworkCore | 10.0.x | Identity + EF Core integration | Ships with ASP.NET Core 10, provides `IdentityDbContext<T>` |
| Microsoft.EntityFrameworkCore.SqlServer | 10.0.10 | SQL Server data provider | Must match .NET 10 exactly |
| Microsoft.EntityFrameworkCore.Tools | 10.0.10 | EF Core migrations tooling | Required for `dotnet ef` CLI |
| Microsoft.EntityFrameworkCore.Design | 10.0.10 | Design-time DbContext | Required in Host.MVC for migration generation |

### Supporting (Phase 1)

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| FluentValidation | 12.x | Request/command validation | Use in Application layer validators; replaces DataAnnotations for complex rules |
| Ardalis.GuardClauses | 4.x | Guard clause utilities | Use in Domain for entity invariant validation |
| Serilog.AspNetCore | 4.x | Structured logging | Replace default `ILogger` with structured logging |

### Frontend (Phase 1)

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Tailwind CSS | v4.x via `@tailwindcss/cli` | Utility-first CSS | Use `@import "tailwindcss"` + `@source` directive, NO `tailwind.config.js` |
| jQuery | 3.7.x | AJAX + DOM manipulation | Use for AJAX calls, form enhancements |

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| IdentityDbContext | Plain DbContext + manual Identity tables | More control but massive boilerplate; never recommended |
| Areas for Admin/Customer | Feature folders in root | Areas provide natural permission boundaries; feature folders cause route collision |
| Scaffold Identity Pages | Custom Razor Pages from scratch | Scaffold gives working auth in minutes; custom from scratch adds days of work |
| IFormFile for pet photos | Direct stream reading via Request.Body | IFormFile is simpler for small files; streaming is for large files only |

**Installation:**
```bash
# Infrastructure layer
dotnet add PetPlatform.Infrastructure package Microsoft.EntityFrameworkCore.SqlServer --version 10.0.10
dotnet add PetPlatform.Infrastructure package Microsoft.EntityFrameworkCore.Tools --version 10.0.10
dotnet add PetPlatform.Infrastructure package Microsoft.AspNetCore.Identity.EntityFrameworkCore --version 10.0.*
dotnet add PetPlatform.Infrastructure package Serilog.AspNetCore --version 4.*

# Application layer
dotnet add PetPlatform.Application package FluentValidation --version 12.*
dotnet add PetPlatform.Application package FluentValidation.DependencyInjectionExtensions --version 12.*
dotnet add PetPlatform.Application package Ardalis.GuardClauses --version 4.*

# Domain layer
dotnet add PetPlatform.Domain package Ardalis.GuardClauses --version 4.*

# Host.MVC
dotnet add PetPlatform.Host.MVC package Microsoft.EntityFrameworkCore.Design --version 10.0.10

# Frontend
cd PetPlatform.Host.MVC
npm init -y
npm install -D @tailwindcss/cli tailwindcss
```

**Version verification:** .NET SDK 10.0.301 confirmed available on machine. EF Core 10.0.10 is latest stable. All package versions verified against NuGet registry via STACK.md research.

## Package Legitimacy Audit

| Package | Registry | Age | Downloads | Source Repo | Verdict | Disposition |
|---------|----------|-----|-----------|-------------|---------|-------------|
| Microsoft.EntityFrameworkCore.SqlServer | NuGet | 12+ yrs | 500M+ | github.com/dotnet/efcore | OK | Approved |
| Microsoft.AspNetCore.Identity.EntityFrameworkCore | NuGet | 12+ yrs | 300M+ | github.com/dotnet/aspnetcore | OK | Approved |
| FluentValidation | NuGet | 14+ yrs | 200M+ | github.com/FluentValidation/FluentValidation | OK | Approved |
| Ardalis.GuardClauses | NuGet | 8+ yrs | 50M+ | github.com/ardalis/ guard-clauses | OK | Approved |
| Serilog.AspNetCore | NuGet | 10+ yrs | 150M+ | github.com/serilog | OK | Approved |
| Microsoft.EntityFrameworkCore.Design | NuGet | 12+ yrs | 300M+ | github.com/dotnet/efcore | OK | Approved |
| Microsoft.EntityFrameworkCore.Tools | NuGet | 12+ yrs | 300M+ | github.com/dotnet/efcore | OK | Approved |
| @tailwindcss/cli | npm | 2+ yrs | 10M+ | github.com/tailwindlabs/tailwindcss | OK | Approved |
| tailwindcss | npm | 8+ yrs | 30M+ | github.com/tailwindlabs/tailwindcss | OK | Approved |
| jquery | npm | 15+ yrs | 100M+ | github.com/jquery/jquery | OK | Approved |

**Packages removed due to [SLOP] verdict:** None
**Packages flagged as suspicious [SUS]:** None

All packages are well-established, high-download-count packages from known organizations. No legitimacy concerns.

## Architecture Patterns

### System Architecture Diagram

```
[Browser Request]
    ↓
[Host.MVC Layer]
    ├─ [Areas/Identity] ← Scaffolded Razor Pages (Register, Login, ConfirmEmail, ForgotPassword)
    ├─ [Areas/Admin] ← Admin controllers (UserManagement, RoleManagement)
    ├─ [Areas/Customer] ← Customer controllers (MyAccount, MyPets)
    └─ [Controllers] ← Home, Pet (public)
    ↓ (calls via DI)
[Application Layer]
    ├─ PetService (CRUD + validation)
    ├─ CustomerService (profile management)
    └─ DTOs, Validators (FluentValidation)
    ↓ (calls via interfaces)
[Infrastructure Layer]
    ├─ ApplicationDbContext : IdentityDbContext<ApplicationUser>
    ├─ UnitOfWork : IUnitOfWork
    ├─ Repositories (PetRepository, CustomerProfileRepository)
    ├─ SeedData (roles, permissions, admin user)
    └─ FileStorageService (pet photos)
    ↓ (EF Core maps to SQL)
[SQL Server]
```

### Recommended Project Structure

```
src/
├── PetPlatform.Domain/
│   ├── Entities/
│   │   ├── Pet.cs
│   │   ├── CustomerProfile.cs
│   │   └── PetPhoto.cs
│   ├── Enums/
│   │   └── PetSpecies.cs
│   └── Interfaces/
│       ├── IRepository.cs
│       └── IUnitOfWork.cs
├── PetPlatform.Application/
│   ├── Services/
│   │   ├── PetService.cs
│   │   └── CustomerService.cs
│   ├── DTOs/
│   │   ├── PetDto.cs
│   │   └── CustomerProfileDto.cs
│   ├── Interfaces/
│   │   ├── IPetService.cs
│   │   └── ICustomerService.cs
│   ├── Validators/
│   │   ├── CreatePetValidator.cs
│   │   └── UpdateCustomerProfileValidator.cs
│   └── Common/
│       └── Result.cs
├── PetPlatform.Infrastructure/
│   ├── Persistence/
│   │   ├── ApplicationDbContext.cs
│   │   ├── Configurations/
│   │   │   ├── PetConfiguration.cs
│   │   │   └── CustomerProfileConfiguration.cs
│   │   └── Migrations/
│   ├── Identity/
│   │   ├── ApplicationUser.cs
│   │   ├── ApplicationRole.cs
│   │   └── SeedData.cs
│   ├── Repositories/
│   │   ├── UnitOfWork.cs
│   │   ├── PetRepository.cs
│   │   └── CustomerProfileRepository.cs
│   └── Services/
│       ├── FileStorageService.cs
│       └── EmailSender.cs
└── PetPlatform.Host.MVC/
    ├── Areas/
    │   ├── Admin/
    │   │   ├── Controllers/
    │   │   │   ├── DashboardController.cs
    │   │   │   ├── UserController.cs
    │   │   │   └── RoleController.cs
    │   │   └── Views/
    │   ├── Customer/
    │   │   ├── Controllers/
    │   │   │   ├── AccountController.cs
    │   │   │   └── MyPetsController.cs
    │   │   └── Views/
    │   └── Identity/
    │       ├── Pages/
    │       │   └── Account/
    │       │       ├── Register.cshtml(.cs)
    │       │       ├── Login.cshtml(.cs)
    │       │       ├── ConfirmEmail.cshtml(.cs)
    │       │       ├── ForgotPassword.cshtml(.cs)
    │       │       └── Manage/
    │       ├── Views/
    │       └── _ViewImports.cshtml
    ├── Controllers/
    │   └── HomeController.cs
    ├── ViewModels/
    ├── Views/
    │   └── Shared/
    │       ├── _Layout.cshtml
    │       └── _LoginPartial.cshtml
    ├── wwwroot/
    │   ├── css/
    │   └── js/
    ├── Program.cs
    └── appsettings.json
```

### Pattern 1: Identity Setup in Clean Architecture

**What:** `ApplicationUser` extends `IdentityUser` and lives in Infrastructure. `ApplicationDbContext` extends `IdentityDbContext<ApplicationUser>`. The Application layer references the Domain layer only — it never depends on Identity types.

**When to use:** Always in this project. The PROJECT.md constraint mandates 4-layer Clean Architecture.

**Example:**
```csharp
// Source: learn.microsoft.com/aspnet/core/security/authentication/customize-identity-model
// Infrastructure/Identity/ApplicationUser.cs
namespace PetPlatform.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    // Custom properties added as needed
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// Infrastructure/Persistence/ApplicationDbContext.cs
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<Pet> Pets => Set<Pet>();
    public DbSet<CustomerProfile> CustomerProfiles => Set<CustomerProfile>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder); // MUST call first for Identity tables
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
```

**Registration in DI:**
```csharp
// Source: learn.microsoft.com/aspnet/core/security/authentication/customize-identity-model
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddIdentity<ApplicationUser, ApplicationRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();
```

### Pattern 2: Claims-Based Policy Authorization

**What:** Register policies in `Program.cs` that check for specific claims. Controllers/actions use `[Authorize(Policy = "...")]` attribute. Claims are seeded onto roles via `RoleManager.AddClaimAsync`.

**When to use:** For all permission-gated endpoints (Admin dashboard, role management, pet CRUD ownership checks).

**Example:**
```csharp
// Source: learn.microsoft.com/aspnet/core/security/authorization/policies
// Program.cs
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Permission:Users.View", policy =>
        policy.RequireClaim("Permission", "Users.View"))
    .AddPolicy("Permission:Users.Manage", policy =>
        policy.RequireClaim("Permission", "Users.Manage"))
    .AddPolicy("Permission:Roles.Create", policy =>
        policy.RequireClaim("Permission", "Roles.Create"))
    .AddPolicy("Permission:Pets.Manage", policy =>
        policy.RequireClaim("Permission", "Pets.Manage"));

// Controller usage
[Area("Admin")]
[Authorize(Policy = "Permission:Users.View")]
public class UserController : Controller { ... }
```

### Pattern 3: Identity Seeding (Roles, Permissions, Admin User)

**What:** Seed roles and permission claims at application startup using a scoped service provider pattern.

**When to use:** At application startup, before any user requests. Run after `app.Build()`.

**Example:**
```csharp
// Source: learn.microsoft.com/aspnet/core/security/authorization/secure-data
// Infrastructure/Identity/SeedData.cs
public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // Seed roles
        string[] roles = { "Admin", "Customer", "Vet", "ServiceProvider" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new ApplicationRole(role));
            }
        }

        // Seed permissions onto roles
        var adminRole = await roleManager.FindByNameAsync("Admin");
        var permissions = new[] { "Users.View", "Users.Manage", "Roles.Create",
                                   "Roles.Assign", "Pets.Manage", "Products.Manage" };
        foreach (var perm in permissions)
        {
            var claims = await roleManager.GetClaimsAsync(adminRole);
            if (!claims.Any(c => c.Type == "Permission" && c.Value == perm))
            {
                await roleManager.AddClaimAsync(adminRole,
                    new Claim("Permission", perm));
            }
        }

        // Seed admin user
        var adminEmail = "admin@petplatform.com";
        if (await userManager.FindByEmailAsync(adminEmail) == null)
        {
            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(admin, "Admin@12345!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, "Admin");
            }
        }
    }
}

// Program.cs — after app = builder.Build()
using (var scope = app.Services.CreateScope())
{
    await SeedData.InitializeAsync(scope.ServiceProvider);
}
```

### Pattern 4: Area-Based MVC Routing

**What:** Use ASP.NET Core Areas to separate Admin, Customer, and Identity concerns. Area routes go before default routes.

**When to use:** For all role-specific controllers and views in this project.

**Example:**
```csharp
// Source: learn.microsoft.com/aspnet/core/mvc/controllers/areas
// Program.cs
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Controller
[Area("Admin")]
[Route("Admin/[controller]/[action]")]
public class UserController : Controller
{
    [Authorize(Policy = "Permission:Users.View")]
    public IActionResult Index() => View();
}
```

**Shared layout via `_ViewStart.cshtml` per area:**
```cshtml
@* Areas/Admin/Views/_ViewStart.cshtml *@
@{
    Layout = "_AdminLayout";
}
```

### Pattern 5: Pet Photo Upload (IFormFile)

**What:** Accept pet photos via `IFormFile`, validate extension/size, store to disk, save relative path in DB.

**When to use:** For PET-01 (create pet with photo) and PET-02 (edit pet photo).

**Example:**
```csharp
// Source: learn.microsoft.com/aspnet/core/mvc/models/file-uploads
// Controller action
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(CreatePetViewModel model)
{
    if (!ModelState.IsValid) return View(model);

    var pet = new Pet
    {
        Name = model.Name,
        Species = model.Species,
        Breed = model.Breed,
        Age = model.Age,
        Weight = model.Weight,
        OwnerId = _userManager.GetUserId(User)
    };

    if (model.Photo != null && model.Photo.Length > 0)
    {
        var permittedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var extension = Path.GetExtension(model.Photo.FileName).ToLowerInvariant();

        if (!permittedExtensions.Contains(extension))
        {
            ModelState.AddModelError("Photo", "Only JPG, PNG, and WebP images are allowed.");
            return View(model);
        }

        if (model.Photo.Length > 5 * 1024 * 1024) // 5MB max
        {
            ModelState.AddModelError("Photo", "Photo must be less than 5MB.");
            return View(model);
        }

        var safeFileName = $"{Guid.NewGuid()}{extension}";
        var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "pets");
        Directory.CreateDirectory(uploadsDir);

        using var stream = new FileStream(Path.Combine(uploadsDir, safeFileName), FileMode.Create);
        await model.Photo.CopyToAsync(stream);

        pet.PhotoPath = $"/uploads/pets/{safeFileName}";
    }

    await _petService.CreateAsync(pet);
    return RedirectToAction(nameof(Index));
}
```

**Razor view form:**
```html
<!-- enctype is REQUIRED for file uploads -->
<form asp-action="Create" method="post" enctype="multipart/form-data">
    @Html.AntiForgeryToken()
    <!-- form fields -->
    <input asp-for="Photo" type="file" accept=".jpg,.jpeg,.png,.webp" />
    <button type="submit">Create Pet</button>
</form>
```

### Anti-Patterns to Avoid

- **Business logic in controllers:** Controllers parse input → call Application service → map result → return View. All business logic belongs in Application services.
- **Using `AddDefaultIdentity` instead of `AddIdentity`:** `AddDefaultIdentity` includes the Identity UI RCL and default token providers. For Clean Architecture with scaffolded pages, use `AddIdentity<ApplicationUser, ApplicationRole>()` and explicitly call `.AddDefaultUI()` if needed.
- **Skipping `base.OnModelCreating(builder)` in IdentityDbContext:** Identity tables will not be created. Always call `base.OnModelCreating(builder)` first.
- **Storing uploaded images in wwwroot:** Images uploaded to wwwroot can be lost on redeploy in cloud environments. Store in a dedicated uploads folder outside wwwroot, or use blob storage. For Phase 1, local filesystem is acceptable.
- **Using client-supplied filenames:** Always generate server-side filenames (`Guid.NewGuid()`) to prevent path traversal and overwrites.
- **Seeding Identity with `AddDefaultIdentity` + `AddRoles`:** If using `AddDefaultIdentity`, the `RoleManager<IdentityRole>` may not resolve correctly. Use `AddIdentity<ApplicationUser, ApplicationRole>()` instead.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Password hashing | Custom hash + salt | ASP.NET Core Identity built-in | Identity uses PBKDF2 with configurable iterations |
| Email confirmation tokens | Custom token generation | `UserManager.GenerateEmailConfirmationTokenAsync()` | Built-in, data protection, one-day timeout |
| Account lockout | Manual lock/unlock logic | `UserManager.SetLockoutEnabledAsync()` + `IdentityOptions.Lockout` | Built-in failed attempt counting, lockout windows |
| Role management UI | Custom CRUD for roles | Scaffolded Identity + custom Admin controllers | Identity provides `RoleManager<T>` for all operations |
| Password reset flow | Custom token + email flow | `UserManager.GeneratePasswordResetTokenAsync()` | Built-in with data protection |
| Cookie authentication | Manual cookie management | `AddIdentity` configures cookie auth automatically | Handles sliding expiration, security stamps, etc. |

**Key insight:** ASP.NET Core Identity provides production-ready authentication flows out of the box. Scaffold and customize — do not rebuild from scratch.

## Common Pitfalls

### Pitfall 1: Identity Middleware Order
**What goes wrong:** `UseAuthorization` placed before `UseAuthentication`, or static files placed after routing, breaking the entire auth pipeline.
**Why it happens:** Copy-paste from tutorials without understanding middleware semantics.
**How to avoid:** Follow canonical order: ExceptionHandling → HTTPS → StaticFiles → Routing → CORS → Authentication → Authorization → Endpoints.
**Warning signs:** 401 errors with valid credentials; CORS errors that appear random.

### Pitfall 2: Not Calling `base.OnModelCreating()` in IdentityDbContext
**What goes wrong:** Identity tables (AspNetUsers, AspNetRoles, etc.) are not created because `IdentityDbContext.OnModelCreating` was never called.
**Why it happens:** Developers override `OnModelCreating` without calling `base` first.
**How to avoid:** Always call `base.OnModelCreating(builder)` as the first line in your override.
**Warning signs:** Migration generates no Identity tables; `dotnet ef database update` creates empty schema.

### Pitfall 3: `AddDefaultIdentity` vs `AddIdentity` Confusion
**What goes wrong:** Using `AddDefaultIdentity` causes `RoleManager<IdentityRole>` to not resolve in DI, because `AddDefaultIdentity` doesn't add role support by default. Or using `AddIdentity` without `.AddRoles<ApplicationRole>()` causes the same issue.
**Why it happens:** Different Identity registration methods have different default behaviors.
**How to avoid:** Use `services.AddIdentity<ApplicationUser, ApplicationRole>().AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders()`.
**Warning signs:** `InvalidOperationException: No service for type 'RoleManager<ApplicationRole>'` at startup.

### Pitfall 4: File Upload Without Server-Side Validation
**What goes wrong:** Client accepts any file type and any size; server stores malicious executables or exhausts disk/memory.
**Why it happens:** Client-side validation alone is insufficient; server must validate independently.
**How to avoid:** Always validate: extension whitelist, file signature check, size limit (5MB for pet photos), and use `Path.GetRandomFileName()` for storage names.
**Warning signs:** Users uploading .exe or .php files; disk space exhaustion.

### Pitfall 5: Missing `_ViewImports.cshtml` in Areas
**What goes wrong:** Tag Helpers don't work in Area views; `@inject`, `@using` directives are missing.
**Why it happens:** `/Views/_ViewImports.cshtml` does NOT automatically apply to Area views.
**How to avoid:** Copy `_ViewImports.cshtml` to each Area's Views folder, or move it to the application root.
**Warning signs:** Tag Helpers render as literal text; `@model` directives fail.

### Pitfall 6: Identity Email Confirmation Not Requiring Confirmed Account
**What goes wrong:** Users can sign in immediately after registration without confirming their email, defeating the purpose of email verification.
**Why it happens:** `RequireConfirmedAccount = true` must be set explicitly in Identity options.
**How to avoid:** Set `options.SignIn.RequireConfirmedAccount = true` in Identity configuration.
**Warning signs:** Users log in before clicking email confirmation link.

### Pitfall 7: Admin Area Accessible Without Authorization
**What goes wrong:** Admin controllers are accessible to any authenticated user or even anonymous users.
**Why it happens:** Missing `[Authorize]` attribute on Admin controllers; Area routes don't automatically enforce auth.
**How to avoid:** Add `[Authorize(Policy = "Permission:...")]` on every Admin controller. Add a fallback policy requiring authentication for the entire app.
**Warning signs:** Non-admin users can access `/Admin/User/Index`.

## Runtime State Inventory

> Not applicable for Phase 1 (greenfield phase — no existing runtime state to inventory).

## Code Examples

### Domain Entity with Private Setters and Invariants

```csharp
// PetPlatform.Domain/Entities/Pet.cs
namespace PetPlatform.Domain.Entities;

public class Pet
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public PetSpecies Species { get; private set; }
    public string? Breed { get; private set; }
    public int Age { get; private set; }
    public decimal Weight { get; private set; }
    public string? PhotoPath { get; set; } // settable for file upload convenience
    public string OwnerId { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private Pet() { } // EF Core

    public static Pet Create(string name, PetSpecies species, string ownerId,
                             string? breed = null, int age = 0, decimal weight = 0)
    {
        Guard.AgNullOrWhiteSpace(name, nameof(name));
        Guard.AgNullOrWhiteSpace(ownerId, nameof(ownerId));
        Guard.Agegative(age, nameof(age)); // Age cannot be negative
        Guard.Agegative(weight, nameof(weight)); // Weight cannot be negative

        return new Pet
        {
            Name = name,
            Species = species,
            OwnerId = ownerId,
            Breed = breed,
            Age = age,
            Weight = weight,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateDetails(string name, string? breed, int age, decimal weight)
    {
        Guard.AgNullOrWhiteSpace(name, nameof(name));
        Guard.Agegative(age, nameof(age));
        Guard.Agegative(weight, nameof(weight));

        Name = name;
        Breed = breed;
        Age = age;
        Weight = weight;
        UpdatedAt = DateTime.UtcNow;
    }
}
```

### Result<T> Pattern

```csharp
// PetPlatform.Application/Common/Result.cs
namespace PetPlatform.Application.Common;

public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }

    private Result(bool isSuccess, T? value, string? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(string error) => new(false, default, error);
}
```

### FluentValidation Validator

```csharp
// PetPlatform.Application/Validators/CreatePetValidator.cs
namespace PetPlatform.Application.Validators;

public class CreatePetValidator : AbstractValidator<CreatePetDto>
{
    public CreatePetValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Pet name is required.")
            .MaximumLength(100).WithMessage("Name must be 100 characters or less.");

        RuleFor(x => x.Species)
            .IsInEnum().WithMessage("Invalid species.");

        RuleFor(x => x.Age)
            .InclusiveBetween(0, 50).WithMessage("Age must be between 0 and 50.");

        RuleFor(x => x.Weight)
            .GreaterThanOrEqualTo(0).WithMessage("Weight cannot be negative.")
            .LessThanOrEqualTo(500).WithMessage("Weight must be less than 500 kg.");
    }
}
```

### Email Sender Stub (Phase 1 — log to console, replace with SMTP later)

```csharp
// PetPlatform.Infrastructure/Services/EmailSender.cs
namespace PetPlatform.Infrastructure.Services;

public class EmailSender : IEmailSender<ApplicationUser>
{
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(ILogger<EmailSender> logger) => _logger = logger;

    public Task SendConfirmationLinkAsync(ApplicationUser user, string email,
        string confirmationLink)
    {
        _logger.LogInformation("Email confirmation link for {Email}: {Link}",
            email, confirmationLink);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email,
        string resetLink)
    {
        _logger.LogInformation("Password reset link for {Email}: {Link}",
            email, resetLink);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email,
        string resetCode)
    {
        _logger.LogInformation("Password reset code for {Email}: {Code}",
            email, resetCode);
        return Task.CompletedTask;
    }
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| `IEmailSender` (non-generic) | `IEmailSender<ApplicationUser>` (generic) | .NET 8+ | Use generic version for `ApplicationUser` in Identity setup |
| `tailwind.config.js` | `@import "tailwindcss"` + `@source` in CSS | Tailwind v4 | No config file; CSS-first configuration |
| `AddDefaultIdentity<T>` | `AddIdentity<TUser, TRole>()` | Always recommended for Clean Architecture | More control over Identity registration; avoids hidden defaults |
| Swashbuckle/Swagger | `Microsoft.AspNetCore.OpenApi` | .NET 9+ | Built-in OpenAPI, no third-party package needed |

**Deprecated/outdated:**
- `Microsoft.jQuery.Unobtrusive.Ajax`: Deprecated, maintenance mode. Use `$.ajax()` directly.
- `tailwindcss` standalone CLI: In v4, CLI is `@tailwindcss/cli`. Using `tailwindcss` directly won't work for builds.
- `AddDefaultIdentity`: For Clean Architecture, prefer `AddIdentity` + explicit role/EF setup.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | SQL Server 2022+ is available for development and production | Environment | Must install SQL Server or use SQLite for development |
| A2 | Email sending is log-only for Phase 1, with real SMTP in later phases | Standard Stack | Must implement full email service if immediate delivery required |
| A3 | Pet photos stored on local filesystem in Phase 1, blob storage in later phases | Code Examples | Must implement cloud storage if immediate production deployment required |
| A4 | Default password policy (6+ chars, 1 non-alphanumeric, 1 uppercase, 1 lowercase) is sufficient | Common Pitfalls | Must customize `IdentityOptions.Password` if stricter policy required |

## Open Questions

1. **SQL Server connection string:** Where is the SQL Server instance running? LocalDB, Docker, or remote?
   - **(RESOLVED)** — Using LocalDB for development: `Server=(localdb)\\mssqllocaldb;Database=PetPlatform;Trusted_Connection=True;MultipleActiveResultSets=true` configured in Plan 01 Task 2 appsettings.json. SQLite fallback documented in Environment Availability table if LocalDB is unavailable.

2. **Arabic RTL support in Phase 1:** The PROJECT.md says "Arabic only for v1." Should Tailwind RTL be configured from Phase 1?
   - **(RESOLVED)** — `dir="rtl"` added to `_Layout.cshtml` html tag in Plan 01 Task 2. Tailwind v4 has native RTL support via `rtl:` variant — RTL-aware classes applied incrementally as views are created. Full Arabic typography and layout polish deferred to later phases.

3. **Identity scaffold scope:** Which Identity pages need customization in Phase 1?
   - **(RESOLVED)** — Using `.AddDefaultUI()` with `Microsoft.AspNetCore.Identity.UI` package for default Identity Razor Pages (Register, Login, ConfirmEmail, ForgotPassword, ResetPassword, Logout). Identity Area `_ViewImports.cshtml` and `_ViewStart.cshtml` created to use shared Tailwind layout. Custom-styled Register/Login pages with Arabic labels planned for a future UI polish phase. Two-factor authentication and external login providers are out of scope for Phase 1.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET 10 SDK | All .NET code | ✓ | 10.0.301 | — |
| SQL Server | Database | ? | Unknown | Use SQLite (`Microsoft.EntityFrameworkCore.Sqlite`) |
| Node.js | Tailwind CSS build | ? | Unknown | Use Tailwind CLI standalone or CDN |
| npm | Tailwind + jQuery install | ? | Unknown | Use `libman` for jQuery, CDN for Tailwind |

**Missing dependencies with no fallback:**
- SQL Server: If not available, planner should include SQLite fallback in `appsettings.Development.json`

**Missing dependencies with fallback:**
- Node.js/npm: If not available, use Tailwind CDN (`<script src="https://cdn.tailwindcss.com">`) as temporary fallback; install Node.js as prerequisite

## Security Domain

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | yes | ASP.NET Core Identity (built-in) |
| V3 Session Management | yes | ASP.NET Core Identity cookie auth |
| V4 Access Control | yes | Policy-based authorization with claims |
| V5 Input Validation | yes | FluentValidation + server-side file validation |
| V6 Cryptography | yes | Identity built-in password hashing (PBKDF2) |

### Known Threat Patterns for ASP.NET Core Identity Stack

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Brute force login | Elevation of Privilege | Account lockout after N failed attempts (`IdentityOptions.Lockout`) |
| CSRF on forms | Tampering | `@Html.AntiForgeryToken()` + `[ValidateAntiForgeryToken]` on all POST |
| Path traversal in file upload | Tampering | Server-generated filenames (`Guid.NewGuid()`), extension whitelist |
| Unauthorized admin access | Information Disclosure | `[Authorize(Policy = "...")]` on every Admin controller |
| Session fixation | Tampering | Identity regenerates security stamp on sign-in |
| Password in logs | Information Disclosure | Never log passwords; Identity doesn't expose them |

## Sources

### Primary (HIGH confidence)
- [learn.microsoft.com/aspnet/core/security/authentication/customize-identity-model](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/customize-identity-model) — ApplicationUser, IdentityDbContext, custom user data
- [learn.microsoft.com/aspnet/core/security/authorization/policies](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/policies) — Policy-based authorization, IAuthorizationRequirement, handlers
- [learn.microsoft.com/aspnet/core/mvc/security/authorization/claims](https://learn.microsoft.com/en-us/aspnet/core/mvc/security/authorization/claims) — Claims-based authorization in MVC
- [learn.microsoft.com/aspnet/core/mvc/controllers/areas](https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/areas) — Areas, routing, shared layouts
- [learn.microsoft.com/aspnet/core/mvc/models/file-uploads](https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads) — IFormFile, validation, storage
- [learn.microsoft.com/aspnet/core/security/authentication/accconfirm](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/accconfirm) — Email confirmation, IEmailSender, token providers
- [learn.microsoft.com/aspnet/core/security/authentication/scaffold-identity](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/scaffold-identity) — Identity scaffolding
- [learn.microsoft.com/aspnet/core/security/authorization/secure-data](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/secure-data) — SeedData pattern, IServiceProvider scope

### Secondary (MEDIUM confidence)
- [github.com/jasontaylordev/CleanArchitecture](https://github.com/jasontaylordev/CleanArchitecture) — Infrastructure layer: ApplicationUser in Infrastructure, IApplicationDbContext
- [cleanarchitecture.jasontaylor.dev/docs/architecture/infrastructure-layer](https://cleanarchitecture.jasontaylor.dev/docs/architecture/infrastructure-layer/) — Infrastructure layer configuration
- [github.com/dotnet/aspnetcore/issues/9895](https://github.com/aspnet/AspNetCore/issues/9895) — Area layout resolution behavior

### Tertiary (LOW confidence)
- StackOverflow answers on Identity seeding — patterns confirmed against official docs
- Blog posts on IUnitOfWork with EF Core — patterns verified against Microsoft eShopOnWeb

## Metadata

**Confidence breakdown:**
- Standard Stack: HIGH — All packages are official Microsoft or well-established NuGet packages. Versions verified against .NET 10 compatibility matrix.
- Architecture: HIGH — Patterns directly from Microsoft Learn official documentation for ASP.NET Core 10.
- Pitfalls: MEDIUM — Based on official docs + community knowledge. Middleware ordering and Identity configuration pitfalls are well-documented.

**Research date:** 2026-07-18
**Valid until:** 2026-08-18 (30 days — stable .NET 10 ecosystem)
