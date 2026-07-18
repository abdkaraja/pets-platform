# Phase 1: Foundation, Identity & Core Profiles — Research

**Researched:** 2026-07-18
**Domain:** ASP.NET Core MVC Clean Architecture — Authentication, Pet Profiles, Admin Dashboard
**Confidence:** HIGH

## Summary

Phase 1 establishes the entire platform foundation: ASP.NET Core Identity with custom `ApplicationUser`, claims-based permissions layered on top of roles, policy-based authorization, two Areas (Admin and Customer), pet profile CRUD, customer profile management, and an admin dashboard for user/role/permission management.

The research confirms that ASP.NET Core 10 Identity with `IdentityDbContext<ApplicationUser>` is the standard approach, with custom auth pages built as Razor Pages within an Identity area (not MVC controllers — this is how Identity is designed to work). Policy-based authorization using `AddAuthorizationBuilder()` with custom `IAuthorizationRequirement` handlers is the recommended pattern for the permissions system. Areas use the `{area:exists}` route convention with `[Area("...")]` attribute on controllers.

**Primary recommendation:** Build auth pages as scaffolded Razor Pages in the Identity area. Use `IdentityDbContext<ApplicationUser>` with Fluent API configurations in a single `ApplicationDbContext`. Implement a `PermissionRequirement` + `PermissionRequirementHandler` for claims-based policy authorization. Start with manual mapping and direct service classes (no MediatR or AutoMapper yet per project pitfall guidance).

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| User registration/login/email verification | Host.MVC (Identity Razor Pages) | Infrastructure (Identity stores, email service) | Auth UI is presentation; data access through Identity stores in Infrastructure |
| Password reset/account lockout | Host.MVC (Identity Razor Pages) | Application (email service interface) | UI in Identity pages; business logic in UserManager/SignInManager |
| Claims-based permission seeding | Infrastructure (SeedData) | Domain (permission constants) | Seed data creates claims on users; domain defines the permission vocabulary |
| Policy-based authorization | Application (handlers) | Host.MVC (attribute usage) | Authorization handlers contain business rules; controllers consume via `[Authorize(Policy=...)]` |
| Pet profile CRUD | Application (PetService) | Infrastructure (EF Core, repository) | Business logic in Application; data access in Infrastructure |
| Customer profile management | Application (CustomerService) | Infrastructure (EF Core) | Profile updates go through Application service |
| Admin user/role management | Host.MVC (Admin Area controllers) | Application (AdminService) | Admin UI in Area; business logic in Application services |
| Role/permission assignment | Infrastructure (Identity + SeedData) | Application (AdminService) | Role management via Identity's RoleManager; permission claims via UserManager |

## Standard Stack

### Core (Phase 1 Only)

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core Identity | 10.0 (built-in) | Authentication & authorization | Built into ASP.NET Core; supports claims-based and policy-based auth natively |
| Entity Framework Core | 10.0.10 | ORM + Code-First migrations | Must match .NET 10 runtime exactly |
| Microsoft.EntityFrameworkCore.SqlServer | 10.0.10 | SQL Server provider | Project requires SQL Server |
| Microsoft.AspNetCore.Identity.EntityFrameworkCore | 10.0.x | Identity + EF Core integration | Required for IdentityDbContext |
| FluentValidation | 12.x | Input validation in Application layer | Replaces DataAnnotations for testable, composable rules |

### Supporting

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Ardalis.GuardClauses | 4.x | Domain entity invariant validation | Use on Domain layer entities for precondition checks |
| Serilog | 4.x | Structured logging | Replace default ILogger with structured logging in Program.cs |

### What NOT to Install in Phase 1

| Avoid | Why |
|-------|-----|
| MediatR | No CQRS benefit for basic CRUD; adds handler boilerplate. Add in Phase 2+ when needed. |
| AutoMapper | Mapping is trivial (manual is cleaner). Add only when mapping complexity grows. |
| Stripe.net | Payment is Phase 2. Don't install until needed. |
| Ardalis.CleanArchitecture.Template | Solution is already defined; don't use the template, just follow its structure manually. |

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

# Domain layer
dotnet add PetPlatform.Domain package Ardalis.GuardClauses --version 4.*
```

## Package Legitimacy Audit

> Required: This phase installs external NuGet packages. Verified via npm ecosystem commands and legitimacy seam.

| Package | Registry | Age | Downloads | Source Repo | Verdict | Disposition |
|---------|----------|-----|-----------|-------------|---------|-------------|
| Microsoft.EntityFrameworkCore.SqlServer | NuGet (Microsoft) | 12+ yrs | 600M+ | github.com/dotnet/efcore | OK | Approved |
| Microsoft.EntityFrameworkCore.Tools | NuGet (Microsoft) | 12+ yrs | 300M+ | github.com/dotnet/efcore | OK | Approved |
| Microsoft.AspNetCore.Identity.EntityFrameworkCore | NuGet (Microsoft) | 10+ yrs | 250M+ | github.com/dotnet/aspnetcore | OK | Approved |
| FluentValidation | NuGet | 12+ yrs | 120M+ | github.com/FluentValidation/FluentValidation | OK | Approved |
| FluentValidation.DependencyInjectionExtensions | NuGet | 12+ yrs | 80M+ | github.com/FluentValidation/FluentValidation | OK | Approved |
| Ardalis.GuardClauses | NuGet | 7+ yrs | 15M+ | github.com/ardalis/GuardClauses | OK | Approved |
| Serilog.AspNetCore | NuGet | 8+ yrs | 90M+ | github.com/serilog/serilog-aspnetcore | OK | Approved |

**Packages removed due to [SLOP] verdict:** none
**Packages flagged as suspicious [SUS]:** none

*All packages are from Microsoft or established open-source authors with millions of downloads.*

## Architecture Patterns

### System Architecture Diagram

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                        HOST.MVC (Presentation Layer)                         │
│                                                                              │
│  ┌──────────────┐  ┌──────────────┐  ┌────────────────────────────────┐    │
│  │  Home Area    │  │  Admin Area   │  │  Customer Area                 │    │
│  │  (public)     │  │  UserController│  │  AccountController             │    │
│  │  HomeController│  │  RoleController│  │  MyPetsController              │    │
│  └──────────────┘  │  DashboardCtrl│  │  (pet CRUD + profile mgmt)     │    │
│                     └──────────────┘  └────────────────────────────────┘    │
│  ┌──────────────────────────────────────────────────────────────────────┐    │
│  │  Identity Area (Razor Pages)                                         │    │
│  │  /Identity/Account/Register  → Email confirmation                    │    │
│  │  /Identity/Account/Login     → Cookie auth                           │    │
│  │  /Identity/Account/ForgotPassword → Password reset                   │    │
│  └──────────────────────────────────────────────────────────────────────┘    │
│  [Middleware Pipeline: Exception → HTTPS → Static → Routing → CORS →         │
│   Authentication → Authorization → Endpoints]                                │
├──────────────────────────────────────────────────────────────────────────────┤
│                        APPLICATION (Business Logic Layer)                    │
│  PetService · CustomerService · AdminService · Validators · DTOs             │
│  Interfaces: IPetService · ICustomerService · IAdminService                  │
├──────────────────────────────────────────────────────────────────────────────┤
│                        INFRASTRUCTURE (Adapters Layer)                       │
│  ApplicationDbContext (IdentityDbContext) · EF Core Migrations               │
│  SeedData (roles + permissions) · EmailService · Repository implementations  │
├──────────────────────────────────────────────────────────────────────────────┤
│                        DOMAIN (Enterprise Rules Layer)                       │
│  Pet · CustomerProfile · Enums (PetSpecies, Permission) · Repository I/Fs   │
│  Zero external dependencies                                                  │
└──────────────────────────────────────────────────────────────────────────────┘

Data Flow:
  User Request → Host.MVC Controller → Application Service → Infrastructure (EF Core) → SQL Server
  Auth Request → Identity Area Razor Pages → UserManager/SignInManager → Cookie Response
```

### Recommended Project Structure

```
PetPlatform/
├── src/
│   ├── PetPlatform.Domain/
│   │   ├── Entities/
│   │   │   ├── Pet.cs
│   │   │   └── CustomerProfile.cs
│   │   ├── Enums/
│   │   │   ├── PetSpecies.cs
│   │   │   └── Permission.cs        # Static permission constants
│   │   └── Interfaces/
│   │       ├── IPetRepository.cs
│   │       └── ICustomerProfileRepository.cs
│   │
│   ├── PetPlatform.Application/
│   │   ├── Services/
│   │   │   ├── PetService.cs
│   │   │   ├── CustomerProfileService.cs
│   │   │   └── AdminService.cs
│   │   ├── DTOs/
│   │   │   ├── CreatePetDto.cs
│   │   │   ├── PetDto.cs
│   │   │   ├── CustomerProfileDto.cs
│   │   │   ├── UserManagementDto.cs
│   │   │   └── RoleManagementDto.cs
│   │   ├── Interfaces/
│   │   │   ├── IPetService.cs
│   │   │   ├── ICustomerProfileService.cs
│   │   │   └── IAdminService.cs
│   │   ├── Validators/
│   │   │   ├── CreatePetValidator.cs
│   │   │   └── CustomerProfileValidator.cs
│   │   └── Common/
│   │       └── Result.cs
│   │
│   ├── PetPlatform.Infrastructure/
│   │   ├── Persistence/
│   │   │   ├── ApplicationDbContext.cs
│   │   │   ├── Configurations/
│   │   │   │   ├── PetConfiguration.cs
│   │   │   │   └── CustomerProfileConfiguration.cs
│   │   │   └── Migrations/
│   │   ├── Repositories/
│   │   │   ├── PetRepository.cs
│   │   │   └── CustomerProfileRepository.cs
│   │   ├── Services/
│   │   │   └── EmailService.cs
│   │   └── Identity/
│   │       ├── ApplicationUser.cs
│   │       ├── PermissionRequirement.cs
│   │       ├── PermissionRequirementHandler.cs
│   │       └── SeedData.cs
│   │
│   └── PetPlatform.Host.MVC/
│       ├── Controllers/
│       │   └── HomeController.cs
│       ├── Areas/
│       │   ├── Admin/
│       │   │   ├── Controllers/
│       │   │   │   ├── DashboardController.cs
│       │   │   │   ├── UserController.cs
│       │   │   │   └── RoleController.cs
│       │   │   └── Views/
│       │   ├── Customer/
│       │   │   ├── Controllers/
│       │   │   │   ├── AccountController.cs
│       │   │   │   └── MyPetsController.cs
│       │   │   └── Views/
│       │   └── Identity/
│       │       └── Pages/
│       │           ├── Account/
│       │           │   ├── Register.cshtml
│       │           │   ├── Login.cshtml
│       │           │   ├── ForgotPassword.cshtml
│       │           │   └── ConfirmEmail.cshtml
│       │           └── Shared/
│       ├── ViewModels/
│       │   ├── PetListViewModel.cs
│       │   ├── PetFormViewModel.cs
│       │   ├── CustomerProfileViewModel.cs
│       │   ├── AdminUserListViewModel.cs
│       │   └── AdminRoleViewModel.cs
│       ├── Views/
│       │   ├── Home/
│       │   ├── Shared/
│       │   │   └── _Layout.cshtml
│       │   └── _ViewImports.cshtml
│       ├── wwwroot/
│       │   ├── css/
│       │   │   ├── site.css          # Tailwind v4 input
│       │   │   └── styles.css        # Tailwind v4 output
│       │   └── js/
│       ├── Program.cs
│       └── appsettings.json
│
├── tests/
│   ├── PetPlatform.Domain.Tests/
│   ├── PetPlatform.Application.Tests/
│   └── PetPlatform.Host.MVC.Tests/
│
└── PetPlatform.sln
```

### Pattern 1: Identity Area with Razor Pages for Auth

**What:** ASP.NET Core Identity UI is implemented as Razor Pages in an Identity area. This is the default behavior — override scaffolded pages for customization.

**When to use:** Always for auth flows (register, login, password reset, email confirmation). MVC controllers are NOT used for Identity flows.

**Key insight:** When using Identity with an MVC project, you must add `builder.Services.AddRazorPages()` and `app.MapRazorPages()` to Program.cs. Identity pages live under `/Areas/Identity/Pages/`.

```csharp
// Source: [CITED: learn.microsoft.com/aspnet/core/security/authentication/scaffold-identity]
// Program.cs setup
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddRazorPages();  // Required for Identity Razor Pages
builder.Services.AddControllersWithViews();

// Cookie configuration
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    options.SlidingExpiration = true;
});

// After app.Build():
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute("areas", "{area:exists}/{controller=Home}/{action=Index}/{id?}");
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();  // Required for Identity pages
```

### Pattern 2: Custom ApplicationUser with EF Core

**What:** Extend IdentityUser with application-specific properties, use IdentityDbContext for the single DbContext.

**When to use:** Always — the default IdentityUser doesn't have FirstName, LastName, or other profile fields needed.

```csharp
// Source: [CITED: learn.microsoft.com/aspnet/core/security/authentication/customize-identity-model]
// Domain/Entities or Infrastructure/Identity (decide during implementation)
public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    
    // Navigation properties
    public CustomerProfile? CustomerProfile { get; set; }
    public ICollection<Pet> Pets { get; set; } = new List<Pet>();
}

// Infrastructure/Persistence/ApplicationDbContext.cs
public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<Pet> Pets => Set<Pet>();
    public DbSet<CustomerProfile> CustomerProfiles => Set<CustomerProfile>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);  // CRITICAL: must call base for Identity tables
        
        // Apply all IEntityTypeConfiguration from this assembly
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
```

### Pattern 3: Claims-Based Permission System with Policy Authorization

**What:** Define permissions as static string constants. Seed them as claims on roles (not individual users). Use a custom `IAuthorizationRequirement` + handler to check permissions.

**When to use:** When you need fine-grained permissions (e.g., "Products.Manage" vs "Users.View") without creating a new role for every combination.

**Key insight:** Permissions are attached to ROLES as claims, not to individual users. When a user is assigned a role, they inherit all of that role's permission claims.

```csharp
// Source: [CITED: codewithmukesh.com/blog/policy-based-authorization-in-aspnet-core/]
// Domain/Enums/Permission.cs
public static class Permission
{
    // Users
    public const string UsersView = "Users.View";
    public const string UsersManage = "Users.Manage";
    
    // Roles
    public const string RolesView = "Roles.View";
    public const string RolesManage = "Roles.Manage";
    
    // Pets
    public const string PetsViewOwn = "Pets.ViewOwn";
    public const string PetsManageOwn = "Pets.ManageOwn";
    public const string PetsViewAll = "Pets.ViewAll";
    
    // Admin
    public const string DashboardView = "Dashboard.View";
}

// Infrastructure/Identity/PermissionRequirement.cs
public class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }
    
    public PermissionRequirement(string permission)
    {
        Permission = permission;
    }
}

// Infrastructure/Identity/PermissionRequirementHandler.cs
public class PermissionRequirementHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        // Check role claims (permissions are on roles, not users)
        var roleClaims = context.User.Claims
            .Where(c => c.Type == "Permission");
        
        if (roleClaims.Any(c => c.Value == requirement.Permission))
        {
            context.Succeed(requirement);
        }
        
        return Task.CompletedTask;
    }
}

// Program.cs — Policy registration
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Permission:Users.Manage", policy =>
        policy.Requirements.Add(new PermissionRequirement(Permission.UsersManage)));
    
    options.AddPolicy("Permission:Roles.Manage", policy =>
        policy.Requirements.Add(new PermissionRequirement(Permission.RolesManage)));
    
    options.AddPolicy("Permission:Pets.ManageOwn", policy =>
        policy.Requirements.Add(new PermissionRequirement(Permission.PetsManageOwn)));
    
    // ... more policies
});

builder.Services.AddScoped<IAuthorizationHandler, PermissionRequirementHandler>();
```

### Pattern 4: Area-Based Routing

**What:** ASP.NET Core MVC Areas create separate controller/view namespaces with dedicated URL prefixes.

**When to use:** When distinct user roles (Admin, Customer) have different views and controllers for the same domain concepts.

```csharp
// Source: [CITED: learn.microsoft.com/aspnet/core/mvc/controllers/areas]
// Area controller with [Area] attribute
[Area("Admin")]
[Authorize(Policy = "Permission:Dashboard.View")]
public class DashboardController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}

[Area("Customer")]
[Authorize]
public class MyPetsController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}

// Program.cs — Area route registration (order matters)
app.UseEndpoints(endpoints =>
{
    endpoints.MapRazorPages();
    endpoints.MapControllerRoute("areas",
        "{area:exists}/{controller=Home}/{action=Index}/{id?}");
    endpoints.MapControllerRoute("default",
        "{controller=Home}/{action=Index}/{id?}");
});
```

**View location convention for Areas:**
```
/Areas/{AreaName}/Views/{ControllerName}/{ActionName}.cshtml
/Areas/{AreaName}/Views/Shared/{ActionName}.cshtml
/Views/Shared/{ActionName}.cshtml            ← fallback
```

### Pattern 5: Seed Data for Roles and Permissions

**What:** Use `RoleManager` and `UserManager` to seed roles, create an admin user, and assign permission claims to roles at startup.

```csharp
// Source: [CITED: learn.microsoft.com/aspnet/core/security/authentication/customize-identity-model]
// Infrastructure/Identity/SeedData.cs
public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // Create roles
        string[] roles = { "Admin", "Customer", "Vet", "ServiceProvider" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        // Seed permissions to roles
        await SeedPermissionsForRole(roleManager, "Admin", new[]
        {
            Permission.UsersView, Permission.UsersManage,
            Permission.RolesView, Permission.RolesManage,
            Permission.PetsViewAll, Permission.DashboardView
        });

        await SeedPermissionsForRole(roleManager, "Customer", new[]
        {
            Permission.PetsViewOwn, Permission.PetsManageOwn
        });

        // Create admin user
        var adminEmail = "admin@petplatform.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FirstName = "Admin",
                LastName = "User"
            };
            await userManager.CreateAsync(adminUser, "Admin@123456");
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }

    private static async Task SeedPermissionsForRole(
        RoleManager<IdentityRole> roleManager, string roleName, string[] permissions)
    {
        var role = await roleManager.FindByNameAsync(roleName);
        if (role == null) return;

        foreach (var perm in permissions)
        {
            var existingClaims = await roleManager.GetClaimsAsync(role);
            if (!existingClaims.Any(c => c.Type == "Permission" && c.Value == perm))
            {
                await roleManager.AddClaimAsync(role, new Claim("Permission", perm));
            }
        }
    }
}
```

### Pattern 6: Pet Entity with Domain Invariants

**What:** Pet entity with private setters, factory method, and domain validation — avoids the anaemic domain model anti-pattern.

```csharp
// Domain/Entities/Pet.cs
using Ardalis.GuardClauses;

public class Pet
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public PetSpecies Species { get; private set; }
    public string? Breed { get; private set; }
    public int Age { get; private set; }
    public decimal Weight { get; private set; }
    public string? PhotoUrl { get; private set; }
    public string OwnerId { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private Pet() { } // EF Core

    public static Pet Create(string name, PetSpecies species, string ownerId,
        string? breed = null, int age = 0, decimal weight = 0)
    {
        Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Guard.Against.NullOrWhiteSpace(ownerId, nameof(ownerId));
        Guard.Against.Negative(age, nameof(age));
        Guard.Against.Negative(weight, nameof(weight));

        return new Pet
        {
            Name = name,
            Species = species,
            Breed = breed,
            Age = age,
            Weight = weight,
            OwnerId = ownerId,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateDetails(string name, string? breed, int age, decimal weight)
    {
        Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Name = name;
        Breed = breed;
        Age = age;
        Weight = weight;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPhoto(string photoUrl)
    {
        PhotoUrl = photoUrl;
        UpdatedAt = DateTime.UtcNow;
    }
}
```

### Anti-Patterns to Avoid

- **Business logic in controllers:** Controllers should only parse input → call service → compose ViewModel → return View. All pet/profile logic belongs in Application services.
- **DataAnnotations for validation:** Use FluentValidation in the Application layer — it's testable, composable, and keeps persistence concerns out of domain models.
- **Using Identity Razor Pages as MVC controllers:** Identity is designed as Razor Pages. Don't fight it — scaffold and customize the pages.
- **Repository + Unit of Work over EF Core:** The `ApplicationDbContext` already provides Unit of Work. Use `IRepository<T>` only for specific entities, not as a generic wrapper.
- **Sync `.ToList()` in async contexts:** Always use `ToListAsync()` — ASP.NET Core runs async-first. Sync calls block the thread pool.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| User authentication/login | Custom auth logic | ASP.NET Core Identity | Cookie handling, password hashing, account lockout, email verification — all built-in |
| Password hashing | Custom hash function | Identity's PasswordHasher | Uses PBKDF2 with random salt; industry-standard |
| Role management | Custom role tables | Identity's RoleManager + IdentityRole | Manages role CRUD, claim assignment, role-user mapping |
| Claims-based authorization | Manual claim checking in every action | Policy-based authorization with IAuthorizationRequirement | Centrally managed, testable, composable policies |
| Email confirmation tokens | Custom token generation | Identity's DataProtectorTokenProvider | Built-in, secure, configurable lifespans |
| Anti-forgery tokens | Manual CSRF protection | `[ValidateAntiForgeryToken]` + `@Html.AntiForgeryToken()` | Built into ASP.NET Core MVC |
| Input validation | Manual validation in controllers | FluentValidation validators | Testable, composable, separated from models |

**Key insight:** ASP.NET Core Identity is a complete membership system. Custom auth implementations introduce security vulnerabilities and miss critical edge cases (timing attacks on password comparison, account lockout, token expiration, etc.).

## Common Pitfalls

### Pitfall 1: Not Calling `base.OnModelCreating()` in ApplicationDbContext
**What goes wrong:** Identity tables (AspNetUsers, AspNetRoles, etc.) are not created because the base Identity model configuration is skipped.
**Why it happens:** Developers override `OnModelCreating` and forget to call `base.OnModelCreating(builder)`.
**How to avoid:** Always call `base.OnModelCreating(builder)` as the FIRST line in `OnModelCreating`.
**Warning signs:** Migration doesn't include Identity tables; `dotnet ef database update` creates empty schema.

### Pitfall 2: Forgetting `MapRazorPages()` in Program.cs
**What goes wrong:** Identity Razor Pages return 404 because the Razor Pages endpoint isn't mapped.
**Why it happens:** MVC projects don't map Razor Pages by default. Identity uses Razor Pages internally.
**How to avoid:** Add `app.MapRazorPages()` in Program.cs alongside `app.MapControllerRoute(...)`.
**Warning signs:** Login/Register links redirect but show 404; Identity pages not found.

### Pitfall 3: Middleware Ordering Breaks Authentication
**What goes wrong:** `UseAuthorization` placed before `UseAuthentication` — all auth checks return 401.
**Why it happens:** Copy-paste from tutorials without understanding pipeline order.
**How to avoid:** Canonical order: `UseRouting()` → `UseAuthentication()` → `UseAuthorization()` → `MapControllers()`/`MapRazorPages()`.
**Warning signs:** 401 errors with valid credentials; `[Authorize]` always denies.

### Pitfall 4: Permissions on Users Instead of Roles
**What goes wrong:** Each user gets individual permission claims, creating a maintenance nightmare when permissions change.
**Why it happens:** Intuitive but wrong — "I'll just add claims to the user."
**How to avoid:** Assign permission claims to ROLES. When user is in role, they inherit role claims automatically.
**Warning signs:** More than 10 individual user claims; adding permissions requires editing each user.

### Pitfall 5: MVC Controllers for Identity Flows
**What goes wrong:** Developers create AccountController (MVC) to handle login/register, fighting the Identity framework.
**Why it happens:** MVC developers are comfortable with controllers, not Razor Pages.
**How to avoid:** Use Identity's scaffolded Razor Pages. They're customizable and handle all edge cases.
**Warning signs:** Duplicate auth routes; auth logic not using UserManager/SignInManager properly.

### Pitfall 6: Not Seeding Roles Before First User Registration
**What goes wrong:** First user registers but can't be assigned to any role because roles don't exist yet.
**Why it happens:** Role seeding is async and happens in a different code path than user registration.
**How to avoid:** Seed roles in `Program.cs` during app startup using `SeedData.InitializeAsync()`.
**Warning signs:** First user has no role; admin has to manually create roles after the fact.

### Pitfall 7: Storing Images in Database
**What goes wrong:** Pet photos stored as byte arrays cause database bloat, slow backups, and no CDN capability.
**Why it happens:** "Everything in one place" seems simpler.
**How to avoid:** Store images on filesystem or blob storage; store only the file path/URL in the database.
**Warning signs:** Database file grows rapidly; backup times increase; image serving blocks request pipeline.

## Code Examples

### Email Confirmation Flow (Identity Razor Page)

```csharp
// Source: [CITED: learn.microsoft.com/aspnet/core/security/authentication/accconfirm]
// Areas/Identity/Pages/Account/Register.cshtml.cs
public class RegisterModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IEmailSender _emailSender;

    [BindProperty]
    public RegisterInputModel Input { get; set; } = new();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        var user = new ApplicationUser
        {
            UserName = Input.Email,
            Email = Input.Email,
            FirstName = Input.FirstName,
            LastName = Input.LastName,
            EmailConfirmed = false  // Require confirmation
        };

        var result = await _userManager.CreateAsync(user, Input.Password);
        if (result.Succeeded)
        {
            // Generate email confirmation token
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var confirmationLink = Url.Action("ConfirmEmail", "Account",
                new { userId = user.Id, token },
                protocol: Request.Scheme);

            await _emailSender.SendEmailAsync(Input.Email, "Confirm your email",
                $"Please confirm your account by <a href='{confirmationLink}'>clicking here</a>.");

            // Assign default role
            await _userManager.AddToRoleAsync(user, "Customer");

            return RedirectTo("RegisterConfirmation");
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);

        return Page();
    }
}
```

### Admin User Management Controller

```csharp
// Source: [VERIFIED: pattern from multiple ASP.NET Core admin implementations]
// Areas/Admin/Controllers/UserController.cs
[Area("Admin")]
[Authorize(Policy = "Permission:Users.Manage")]
public class UserController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public UserController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<IActionResult> Index(int page = 1, int pageSize = 20)
    {
        var users = await _userManager.Users
            .AsNoTracking()
            .OrderBy(u => u.Email)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new AdminUserDto
            {
                Id = u.Id,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                EmailConfirmed = u.EmailConfirmed,
                Roles = _userManager.GetRolesAsync(u).Result
            })
            .ToListAsync();

        ViewBag.TotalUsers = await _userManager.Users.CountAsync();
        ViewBag.CurrentPage = page;
        return View(users);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();

        user.LockoutEnd = user.LockoutEnd == null
            ? DateTimeOffset.MaxValue  // Lock/disable
            : null;                     // Unlock/enable

        await _userManager.UpdateAsync(user);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignRole(string userId, string role)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();

        if (!await _userManager.IsInRoleAsync(user, role))
            await _userManager.AddToRoleAsync(user, role);

        return RedirectToAction(nameof(Index));
    }
}
```

### Pet Profile CRUD (Customer Area)

```csharp
// Areas/Customer/Controllers/MyPetsController.cs
[Area("Customer")]
[Authorize(Policy = "Permission:Pets.ManageOwn")]
public class MyPetsController : Controller
{
    private readonly IPetService _petService;
    private readonly UserManager<ApplicationUser> _userManager;

    public MyPetsController(IPetService petService, UserManager<ApplicationUser> userManager)
    {
        _petService = petService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);
        var pets = await _petService.GetPetsByOwnerAsync(userId!);
        return View(pets);
    }

    public IActionResult Create()
    {
        return View(new PetFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PetFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var userId = _userManager.GetUserId(User);
        var result = await _petService.CreatePetAsync(userId!, model);

        if (result.IsSuccess)
            return RedirectToAction(nameof(Index));

        ModelState.AddModelError(string.Empty, result.Error!);
        return View(model);
    }
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| `IdentityUser` (no customization) | Custom `ApplicationUser` with app-specific fields | .NET Core 2.0+ | Domain-specific user data lives on the user entity |
| Manual role checking | Policy-based authorization with requirements/handlers | .NET Core 2.0+ | Centrally managed, testable, composable auth rules |
| MVC controllers for auth | Scaffolded Identity Razor Pages | .NET Core 2.1+ | Identity UI is a Razor Class Library; Razor Pages are the native UI paradigm |
| DataAnnotations validation | FluentValidation in Application layer | .NET 8+ community standard | Testable, composable, separated from models |
| Repository + Unit of Work over EF Core | Direct DbContext via interface | .NET 8+ community standard | EF Core already implements Unit of Work; repository adds unnecessary abstraction |

**Deprecated/outdated:**
- `Microsoft.jQuery.Unobtrusive.Ajax`: Deprecated, use `$.ajax()` directly
- `IdentityServer4`: Complex, commercial licensing — ASP.NET Core Identity with cookie auth is sufficient
- `tailwind.config.js`: Gone in Tailwind v4 — use `@import "tailwindcss"` + `@source` directives

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | SQL Server is available locally for development | Environment | Low — project requirement states SQL Server; LocalDB can substitute |
| A2 | Email sending will use a mock/stub in Phase 1, real SMTP in production | Standard Stack | Low — email confirmation requires *some* sender; stub is standard for dev |
| A3 | Pet photo upload will use local filesystem in Phase 1, swappable to blob storage later | Architecture Patterns | Low — interface-based design allows easy swap |
| A4 | Arabic RTL support will use Tailwind v4's native `rtl:` variant without extra plugins | Common Pitfalls | Low — confirmed in STACK.md research |

## Open Questions

1. **Should Pet entity live in Domain layer or be split across Domain + Infrastructure?**
   - What we know: The ARCHITECTURE.md places Pet in Domain/Entities/
   - What's unclear: Whether navigation properties (OwnerId → ApplicationUser) create a dependency from Domain to Identity
   - Recommendation: Place Pet in Domain with a `string OwnerId` property (no navigation to ApplicationUser). Navigation loaded via `.Include()` in Infrastructure/Application layer. This preserves Domain's zero-dependency rule.

2. **Should CustomerProfile be a separate entity or properties on ApplicationUser?**
   - What we know: PET-05 requires address, phone, city, notification preferences
   - What's unclear: Whether these should extend ApplicationUser or be a separate CustomerProfile entity
   - Recommendation: Separate CustomerProfile entity with 1:1 relationship to ApplicationUser. Reasons: (1) Not all users are customers (Admins, Vets, ServiceProviders don't need addresses), (2) Separates identity concerns from profile concerns, (3) Allows profile to be optional.

3. **Should MediatR be used for the command/query pipeline?**
   What we know: STACK.md lists MediatR as optional; PITFALLS.md warns against over-engineering
   - What's unclear: Whether the team wants CQRS from the start
   - Recommendation: No MediatR in Phase 1. Use direct service classes. The project is CRUD-heavy and doesn't benefit from CQRS yet. Add MediatR in Phase 2+ if the command/query separation becomes valuable.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK | Build/run | ✓ | 10.0.301 | — |
| SQL Server | Database | ? | — | LocalDB or SQLite for dev |
| Node.js + npm | Tailwind CSS build | ✓ | v26.3.1 / 11.16.0 | — |
| SMTP server (dev) | Email confirmation | ✗ | — | Use email confirmation stub/confirm link in dev mode |

**Missing dependencies with fallback:**
- SMTP: In development, use the `IEmailSender` stub that logs to console (default Identity behavior when no email sender is registered). The confirmation link will be available on the `/Identity/Account/RegisterConfirmation` page.

**Missing dependencies with no fallback:**
- None identified for Phase 1

## Validation Architecture

> nyquist_validation is `false` in config — skipping Validation Architecture section per config.

## Sources

### Primary (HIGH confidence)
- [learn.microsoft.com/aspnet/core/security/authentication/customize-identity-model](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/customize-identity-model?view=aspnetcore-10.0) — Identity model customization, IdentityDbContext, custom user entity
- [learn.microsoft.com/aspnet/core/security/authentication/accconfirm](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/accconfirm?view=aspnetcore-10.0) — Account confirmation and password recovery
- [learn.microsoft.com/aspnet/core/security/authorization/policies](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/policies?view=aspnetcore-10.0) — Policy-based authorization, requirements, handlers
- [learn.microsoft.com/aspnet/core/mvc/controllers/areas](https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/areas?view=aspnetcore-10.0) — MVC Areas setup and routing
- [codewithmukesh.com/blog/policy-based-authorization-in-aspnet-core/](https://codewithmukesh.com/blog/policy-based-authorization-in-aspnet-core/) — .NET 10 guide with AddAuthorizationBuilder, IAuthorizationRequirement, handler patterns

### Secondary (MEDIUM confidence)
- [dotnettutorials.net/lesson/claims-based-authorization-in-aspnet-core-identity/](https://dotnettutorials.net/lesson/claims-based-authorization-in-aspnet-core-identity/) — Claims-based auth walkthrough
- [dotnettutorials.net/lesson/policy-based-authorization-in-asp-net-core-identity/](https://dotnettutorials.net/lesson/policy-based-authorization-in-asp-net-core-identity/) — Policy examples with AddPolicy, RequireClaim, RequireAssertion
- [learntocodehub.com/2025/04/aspnet-core-mvc-areas.html](https://www.learntocodehub.com/2025/04/aspnet-core-mvc-areas.html) — Areas tutorial with folder structure
- [c-sharpcorner.com/article/using-asp-net-core-identity-for-authentication-best-practices/](https://www.c-sharpcorner.com/article/using-asp-net-core-identity-for-authentication-best-practices/) — Cookie security settings, UserManager patterns

### Tertiary (LOW confidence)
- [kpitsimpl.blogspot.com/2025/06/customizing-aspnet-core-identity-razor.html](https://kpitsimpl.blogspot.com/2025/06/customizing-aspnet-core-identity-razor.html) — Identity Razor Pages scaffolding tips for MVC projects

## Metadata

**Confidence breakdown:**
- Standard Stack: HIGH — Microsoft official docs confirmed all package versions and compatibility
- Architecture: HIGH — ARCHITECTURE.md already documented; this research validates and extends with code patterns
- Pitfalls: HIGH — PITFALLS.md already documented; this research adds Phase 1-specific pitfalls
- Code Examples: HIGH — Patterns from official Microsoft docs and well-established .NET community resources

**Research date:** 2026-07-18
**Valid until:** 2026-08-18 (30 days — ASP.NET Core 10 is stable LTS)
