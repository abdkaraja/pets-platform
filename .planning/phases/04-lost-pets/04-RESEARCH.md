# Phase 4: Lost Pets Module - Research

**Researched:** 2026-07-20
**Domain:** ASP.NET Core MVC / EF Core / Clean Architecture — Lost & Found Pet Reporting
**Confidence:** HIGH

## Summary

This phase adds a Lost Pets module to the Pet Platform, following the same Clean Architecture patterns established in Phases 1-3. The core deliverable is a `LostPetReport` entity (with optional FK to `Pet`) that supports Lost/Found types, photo uploads (1-5), free-text location search, and synchronous match notification on report creation.

The codebase has strong, consistent patterns across all four layers. The Adoption module (Phase 3) is the closest architectural analogue — it demonstrates how a domain entity with optional Pet FK, search/filter DTOs, paginated results, and status lifecycle should be structured. The Lost Pets module will follow these patterns precisely: a `LostPetReport` entity mirroring `AdoptionListing` structure, a `LostPetService` mirroring `AdoptionService`, and Customer-area controllers/views mirroring the adoption UI pattern.

**Primary recommendation:** Mirror the Adoption module's architecture exactly — `LostPetReport` entity → `ILostPetService` → `LostPetController` in Customer area. Add `LostPetReportPhoto` and `MatchNotification` as supporting entities. Matching logic lives in the service layer as a synchronous post-creation step.

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| LOST-01 | User can report lost/found pets with details | `LostPetReport` entity with Create/Update, `IFileStorageService` for 1-5 photos, FluentValidation for required fields (species, color, location, date, description) |
| LOST-02 | User can search lost pets by species, breed, color, location, date | `LostPetReportFilterDto` with Contains-based queries on location, species/breed/color filters, date range — mirrors `AdoptionListingFilterDto` pattern |
| LOST-03 | User receives basic alerts for matching lost pets | `MatchNotification` entity created synchronously on report creation, matching by same species + contains-location, visible only to reporter via notification page |
</phase_requirements>

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Report creation & validation | Application (Service + Validators) | Domain (Entity factory) | Service orchestrates validation + entity creation + photo upload + matching |
| Report search & filtering | Application (Service) | Infrastructure (EF Core queries) | Service builds LINQ query, EF translates to SQL |
| Photo upload & storage | Infrastructure (FileStorageService) | Host.MVC (Controller) | Controller handles IFormFile, delegates to existing IFileStorageService |
| Match detection | Application (Service) | Domain (Entities) | Synchronous match logic queries opposite-type reports on creation |
| Notification display | Application (Service) | Host.MVC (Controller/Views) | Service queries notifications, controller renders view |
| Status management (Open/Resolved) | Domain (Entity methods) | Application (Service) | Domain enforces state transitions, service orchestrates |
| Authorization (resolve/edit) | Host.MVC (Controller) | Application (Service) | Controller checks userId, service enforces ownership |

## Standard Stack

### Core (no new packages needed)

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Entity Framework Core | 9.0.x (existing) | ORM, migrations | Already in project, Code-First pattern established |
| FluentValidation | latest (existing) | Input validation | Already in project, every DTO has a validator |
| Ardalis.GuardClauses | latest (existing) | Domain-level guards | Already used in every entity factory method |
| ASP.NET Core Identity | 9.0.x (existing) | Auth, userId extraction | Already in project |

### Supporting (all already in project)

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| IFileStorageService | existing | Photo upload to wwwroot/uploads/ | When saving 1-5 report photos |
| Result\<T\> | existing | Service error handling | All service methods return Result<T> |
| PagedResultDto\<T\> | existing | Paginated search results | Search/filter endpoint |

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Contains-based location search | Full-text index (SQL Server CONTAINSTABLE) | Overkill for v1 — LIKE queries sufficient for expected data volume |
| Synchronous matching | Background job (IHostedService) | Decision D-08 explicitly chose synchronous; background jobs add complexity |
| Separate LostPetReportPhoto entity | JSON array on LostPetReport | Separate entity is cleaner for EF Core, allows individual photo deletion, follows existing pattern (CartItem, OrderItem are separate entities) |

**Installation:** No new packages. All dependencies already exist in the solution.

## Package Legitimacy Audit

No new external packages are being installed. All dependencies (EF Core, FluentValidation, Ardalis.GuardClauses, ASP.NET Core Identity) are already in the project and have been verified in prior phases.

**Packages removed due to [SLOP] verdict:** none
**Packages flagged as suspicious [SUS]:** none

## Architecture Patterns

### Recommended Project Structure

```
src/
├── PetPlatform.Domain/
│   ├── Entities/
│   │   ├── LostPetReport.cs              # NEW — core entity
│   │   └── LostPetReportPhoto.cs         # NEW — photo references
│   └── Enums/
│       ├── LostPetReportType.cs          # NEW — Lost/Found
│       └── LostPetReportStatus.cs        # NEW — Open/Resolved
│
├── PetPlatform.Application/
│   ├── Interfaces/
│   │   └── ILostPetService.cs            # NEW — service interface
│   ├── DTOs/
│   │   └── LostPetDtos.cs               # NEW — all DTOs in one file (mirrors AdoptionDtos.cs)
│   ├── Services/
│   │   └── LostPetService.cs             # NEW — service implementation
│   └── Validators/
│       ├── CreateLostPetReportValidator.cs  # NEW
│       └── UpdateLostPetReportValidator.cs  # NEW
│
├── PetPlatform.Infrastructure/
│   └── Persistence/
│       ├── Configurations/
│       │   ├── LostPetReportConfiguration.cs     # NEW
│       │   └── LostPetReportPhotoConfiguration.cs # NEW
│       └── ApplicationDbContext.cs               # EDIT — add DbSets
│
└── PetPlatform.Host.MVC/
    └── Areas/Customer/
        ├── Controllers/
        │   └── LostPetController.cs       # NEW
        └── Views/
            └── LostPet/
                ├── Index.cshtml           # Browse/search reports
                ├── Details.cshtml         # View single report
                ├── Create.cshtml          # Report lost/found
                ├── Edit.cshtml            # Edit open report
                ├── MyReports.cshtml       # Reporter's own reports
                └── Notifications.cshtml   # Match alerts
```

### Pattern 1: LostPetReport Entity (mirrors AdoptionListing)

**What:** Core entity with private setters, factory method, status lifecycle methods. Optional PetId FK allows reports without a linked Pet.

**When to use:** When creating the domain entity for lost/found pet reports.

```csharp
// Source: Established pattern from Pet.cs and AdoptionListing.cs
using Ardalis.GuardClauses;
using PetPlatform.Domain.Enums;

namespace PetPlatform.Domain.Entities;

public class LostPetReport
{
    public int Id { get; private set; }
    public string ReporterUserId { get; private set; } = string.Empty;
    public LostPetReportType ReportType { get; private set; }
    public PetSpecies Species { get; private set; }
    public string? Breed { get; private set; }
    public string Color { get; private set; } = string.Empty;
    public string Location { get; private set; } = string.Empty;       // Free-text city/area
    public DateTime DateReported { get; private set; }                 // When pet was lost/found
    public string Description { get; private set; } = string.Empty;
    public int? PetId { get; private set; }                            // Optional FK to Pet
    public LostPetReportStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // Navigation properties
    public Pet? Pet { get; private set; }
    public ICollection<LostPetReportPhoto> Photos { get; private set; } = new List<LostPetReportPhoto>();
    public ICollection<MatchNotification> MatchNotifications { get; private set; } = new List<MatchNotification>();

    private LostPetReport() { } // EF Core

    public static LostPetReport Create(
        string reporterUserId,
        LostPetReportType reportType,
        PetSpecies species,
        string color,
        string location,
        DateTime dateReported,
        string description,
        string? breed = null,
        int? petId = null)
    {
        Guard.Against.NullOrWhiteSpace(reporterUserId, nameof(reporterUserId));
        Guard.Against.NullOrWhiteSpace(color, nameof(color));
        Guard.Against.NullOrWhiteSpace(location, nameof(location));
        Guard.Against.NullOrWhiteSpace(description, nameof(description));

        return new LostPetReport
        {
            ReporterUserId = reporterUserId,
            ReportType = reportType,
            Species = species,
            Breed = breed,
            Color = color,
            Location = location,
            DateReported = dateReported,
            Description = description,
            PetId = petId,
            Status = LostPetReportStatus.Open,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateDetails(
        string color,
        string location,
        DateTime dateReported,
        string description,
        string? breed = null)
    {
        Guard.Against.NullOrWhiteSpace(color, nameof(color));
        Guard.Against.NullOrWhiteSpace(location, nameof(location));
        Guard.Against.NullOrWhiteSpace(description, nameof(description));

        Color = color;
        Location = location;
        DateReported = dateReported;
        Description = description;
        Breed = breed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Resolve()
    {
        if (Status != LostPetReportStatus.Open)
            throw new InvalidOperationException("Only open reports can be resolved.");

        Status = LostPetReportStatus.Resolved;
        UpdatedAt = DateTime.UtcNow;
    }
}
```

### Pattern 2: Service Interface + Result\<T\> (mirrors IAdoptionService)

```csharp
// Source: Established pattern from IAdoptionService.cs
using PetPlatform.Application.Common;
using PetPlatform.Application.DTOs;

namespace PetPlatform.Application.Interfaces;

public interface ILostPetService
{
    // Public-facing
    Task<PagedResultDto<LostPetReportDto>> SearchReportsAsync(LostPetReportFilterDto filter);
    Task<LostPetReportDto?> GetReportByIdAsync(int id);

    // Reporter-facing
    Task<Result<LostPetReportDto>> CreateReportAsync(CreateLostPetReportDto dto, string reporterUserId);
    Task<Result<LostPetReportDto>> UpdateReportAsync(int id, UpdateLostPetReportDto dto, string reporterUserId);
    Task<Result<bool>> ResolveReportAsync(int id, string reporterUserId);
    Task<IEnumerable<LostPetReportDto>> GetMyReportsAsync(string reporterUserId);
    Task<IEnumerable<MatchNotificationDto>> GetMyNotificationsAsync(string userId);
    Task<Result<bool>> MarkNotificationReadAsync(int notificationId, string userId);
}
```

### Pattern 3: Synchronous Matching (D-06, D-08)

**What:** On report creation, the service queries for opposite-type reports with matching species + contains-location, then creates MatchNotification records for those reporters.

**When to use:** Inside `LostPetService.CreateReportAsync`, after saving the new report.

```csharp
// Source: Decision D-06 + D-08 — matching = same species + same city/area
// Synchronous, triggered on report creation

private async Task FindAndCreateMatchesAsync(LostPetReport newReport)
{
    // Find opposite-type open reports with same species
    var oppositeType = newReport.ReportType == LostPetReportType.Lost
        ? LostPetReportType.Found
        : LostPetReportType.Lost;

    var potentialMatches = await _context.LostPetReports
        .Where(r => r.ReportType == oppositeType
                  && r.Status == LostPetReportStatus.Open
                  && r.Species == newReport.Species
                  && r.Location.Contains(newReport.Location))
        .ToListAsync();

    foreach (var match in potentialMatches)
    {
        // Notify the reporter of the existing report about the new match
        if (match.ReporterUserId != newReport.ReporterUserId)
        {
            var notification = MatchNotification.Create(
                match.Id,                           // The existing report being matched
                newReport.Id,                       // The new report that triggered the match
                newReport.ReporterUserId,           // Who created the new report
                $"A {newReport.ReportType.ToString().ToLower()} pet report matches your {match.ReportType.ToString().ToLower()} report: {newReport.Description[..Math.Min(100, newReport.Description.Length)]}...");
            _context.MatchNotifications.Add(notification);
        }
    }

    // Also check: does the new report match any existing report where the *new* reporter should be notified?
    // (i.e., if a user reports a Found pet, and there's already a Lost report by a different user with same species + location)
    var reverseMatches = await _context.LostPetReports
        .Where(r => r.ReportType == oppositeType
                  && r.Status == LostPetReportStatus.Open
                  && r.Species == newReport.Species
                  && r.Location.Contains(newReport.Location)
                  && r.ReporterUserId != newReport.ReporterUserId)
        .ToListAsync();

    foreach (var match in reverseMatches)
    {
        var notification = MatchNotification.Create(
            newReport.Id,
            match.Id,
            match.ReporterUserId,
            $"A {match.ReportType.ToString().ToLower()} pet report matches your {newReport.ReportType.ToString().ToLower()} report: {match.Description[..Math.Min(100, match.Description.Length)]}...");
        _context.MatchNotifications.Add(notification);
    }
}
```

**Note:** The matching logic needs careful attention to avoid duplicate notifications. The design above creates notifications in both directions: (1) notifies existing reporters about the new report, and (2) notifies the new reporter about existing matches. The planner should ensure the service checks for existing notifications before creating duplicates.

### Anti-Patterns to Avoid

- **Don't put auth in the service layer:** Follow existing pattern — controller checks `GetUserId()` and passes to service. Service validates ownership via `ReporterUserId != userId`.
- **Don't skip Guard.Against:** Every entity factory and service method uses Ardalis.GuardClauses for parameter validation.
- **Don't use raw SQL for location search:** Use EF Core `.Contains()` which translates to `LIKE '%value%'` — sufficient for v1 data volumes.
- **Don't create a separate notification service:** Keep it in LostPetService for v1. The matching logic is tightly coupled to report creation.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Photo file storage | Custom file handler | IFileStorageService.SaveFileAsync() | Already handles extension validation, size limits, GUID naming |
| Paginated search results | Manual Skip/Take + count | PagedResultDto\<T\> + existing LINQ pattern | Already proven in AdoptionService.GetActiveListingsAsync |
| Form validation | Manual ModelState.AddModelError | FluentValidation AbstractValidator | Consistent with every other DTO in the project |
| User ID extraction | Custom claims parsing | User.FindFirst(ClaimTypes.NameIdentifier) or _userManager.GetUserId(User) | Both patterns exist in the codebase |
| Status transitions | If/else chains with no guard | Domain entity methods with state validation | AdoptionApplication.UpdateStatus shows the pattern |

## Common Pitfalls

### Pitfall 1: Duplicate Match Notifications
**What goes wrong:** When Report A (Lost) is created and matches Report B (Found), and then Report B's reporter creates another report, duplicate notifications accumulate.
**Why it happens:** The matching query doesn't check for existing notifications between the same two reports.
**How to avoid:** In the matching logic, check `!_context.MatchNotifications.Any(n => n.TriggeredReportId == newReport.Id && n.MatchedReportId == existingReport.Id)` before creating.
**Warning signs:** Users seeing the same match multiple times in their notification list.

### Pitfall 2: Location Contains is Bidirectional
**What goes wrong:** A report with location "New York" matches a report with "New York City" (good), but also "York" if the substring appears elsewhere (false positives).
**Why it happens:** SQL `LIKE '%York%'` matches any string containing "York".
**How to avoid:** This is acceptable for v1 per Decision D-11. The planner should note this as a known limitation. Future improvement: normalize location to city names or use structured location data.

### Pitfall 3: Optional PetId FK Referencing Non-Existent Pet
**What goes wrong:** A report references PetId=5, but that pet is deleted or doesn't exist.
**Why it happens:** The FK is optional but not validated on creation.
**How to avoid:** In `LostPetService.CreateReportAsync`, if `dto.PetId.HasValue`, validate the Pet exists and belongs to the reporter. Set `PetId = null` if not provided.

### Pitfall 4: Photo Count Limit Not Enforced
**What goes wrong:** A user uploads 10 photos when the limit is 1-5.
**Why it happens:** The limit is only enforced in the UI, not in the service.
**How to avoid:** In `CreateReportAsync`, after processing photos, verify `photos.Count >= 1 && photos.Count <= 5`. Return `Result.Failure` if out of range.

### Pitfall 5: Reporter Can Resolve Others' Reports
**What goes wrong:** The authorization check is missed, allowing any authenticated user to resolve any report.
**Why it happens:** The service method forgets to check `ReporterUserId == userId`.
**How to avoid:** Always validate `report.ReporterUserId != userId` returns `Result.Failure("You are not authorized to resolve this report.")`. This pattern is already proven in `PetService.UpdateAsync` and `AdoptionService.CloseListingAsync`.

## Code Examples

### Entity Configuration (mirrors AdoptionListingConfiguration)

```csharp
// Source: Pattern from AdoptionListingConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetPlatform.Domain.Entities;

namespace PetPlatform.Infrastructure.Persistence.Configurations;

public class LostPetReportConfiguration : IEntityTypeConfiguration<LostPetReport>
{
    public void Configure(EntityTypeBuilder<LostPetReport> builder)
    {
        builder.HasKey(lpr => lpr.Id);

        builder.Property(lpr => lpr.ReporterUserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(lpr => lpr.ReportType)
            .HasConversion<int>();

        builder.Property(lpr => lpr.Species)
            .HasConversion<int>();

        builder.Property(lpr => lpr.Breed)
            .HasMaxLength(100);

        builder.Property(lpr => lpr.Color)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(lpr => lpr.Location)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(lpr => lpr.Description)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(lpr => lpr.Status)
            .HasConversion<int>();

        // Optional FK to Pet
        builder.HasOne(lpr => lpr.Pet)
            .WithMany()
            .HasForeignKey(lpr => lpr.PetId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(lpr => lpr.ReporterUserId);
        builder.HasIndex(lpr => lpr.Location);
        builder.HasIndex(lpr => lpr.Species);
        builder.HasIndex(lpr => lpr.Status);
        builder.HasIndex(lpr => new { lpr.Species, lpr.Status });
        builder.HasIndex(lpr => new { lpr.ReportType, lpr.Status, lpr.Species });
    }
}
```

### DbContext Registration

```csharp
// Add to ApplicationDbContext.cs — following existing pattern
// Lost Pets entities
public DbSet<LostPetReport> LostPetReports => Set<LostPetReport>();
public DbSet<LostPetReportPhoto> LostPetReportPhotos => Set<LostPetReportPhoto>();
public DbSet<MatchNotification> MatchNotifications => Set<MatchNotification>();
```

### DI Registration in Program.cs

```csharp
// Add alongside existing service registrations
builder.Services.AddScoped<ILostPetService, LostPetService>();
```

### FluentValidation (mirrors CreateListingValidator)

```csharp
// Source: Pattern from CreateListingValidator.cs
using FluentValidation;
using PetPlatform.Application.DTOs;

namespace PetPlatform.Application.Validators;

public class CreateLostPetReportValidator : AbstractValidator<CreateLostPetReportDto>
{
    public CreateLostPetReportValidator()
    {
        RuleFor(x => x.ReportType)
            .IsInEnum().WithMessage("Invalid report type.");

        RuleFor(x => x.Species)
            .IsInEnum().WithMessage("Invalid species.");

        RuleFor(x => x.Color)
            .NotEmpty().WithMessage("Color is required.")
            .MaximumLength(100).WithMessage("Color must be 100 characters or less.");

        RuleFor(x => x.Location)
            .NotEmpty().WithMessage("Location is required.")
            .MaximumLength(200).WithMessage("Location must be 200 characters or less.");

        RuleFor(x => x.DateReported)
            .NotEmpty().WithMessage("Date is required.")
            .LessThanOrEqualTo(DateTime.UtcNow.AddDays(1)).WithMessage("Date cannot be in the far future.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(2000).WithMessage("Description must be 2000 characters or less.");

        RuleFor(x => x.Breed)
            .MaximumLength(100).WithMessage("Breed must be 100 characters or less.");

        RuleFor(x => x.PetId)
            .GreaterThan(0).When(x => x.PetId.HasValue)
            .WithMessage("PetId must be greater than 0 when provided.");
    }
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| N/A (new feature) | — | — | — |

**Deprecated/outdated:** None — this is a greenfield module.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | No new NuGet packages required — all dependencies already in solution | Standard Stack | Low — if a package is missing, `dotnet restore` will fail immediately |
| A2 | SQL Server LIKE queries via EF Core `.Contains()` are sufficient for location search at v1 scale | Common Pitfalls | Low — performance acceptable for expected data volumes (< 10K reports) |
| A3 | IFileStorageService supports multiple sequential SaveFileAsync calls for the same subdirectory | Code Examples | Low — verified by reading FileStorageService.cs implementation |
| A4 | MatchNotification does not need read/unread tracking beyond a simple boolean | Architecture Patterns | Low — D-07 says alerts visible only to reporter; simple read/unread is sufficient for v1 |

## Open Questions

1. **Notification cleanup for Resolved reports:**
   - What we know: Reports can be resolved (D-12). Notifications reference specific reports.
   - What's unclear: Should notifications be hidden/deleted when a report is resolved?
   - Recommendation: Keep notifications visible after resolution — the reporter may want to see historical matches. Planner should decide whether to filter out notifications for resolved reports or keep them visible.

2. **Match notification deduplication across report edits:**
   - What we know: Reporter can edit while Open (D-15). Matching happens on creation (D-08).
   - What's unclear: If a report is edited (e.g., location changes), should matching re-run?
   - Recommendation: No — matching only on creation per D-08. Edit does not trigger re-matching. Planner should document this as a known behavior.

3. **Photo management on edit:**
   - What we know: Reporter can edit while Open (D-15). Photos are 1-5 (D-04).
   - What's unclear: Can the reporter add/remove photos when editing?
   - Recommendation: Allow adding/removing photos during edit. The planner should design the edit flow to support photo management.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK | All code | ✓ | 9.0.311 | — |
| SQL Server | Database | ✓ (connection string in appsettings) | — | — |
| Node.js | Tailwind CSS build | ✓ | v26.3.1 | — |

**Missing dependencies with no fallback:** None.

## Validation Architecture

> Skipped — `workflow.nyquist_validation` is explicitly `false` in `.planning/config.json`.

## Security Domain

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | no | Already handled by Phase 1 Identity system |
| V3 Session Management | no | Already handled by Phase 1 Identity system |
| V4 Access Control | yes | Reporter-only edit/resolve enforced in service layer; all users can browse (D-16) |
| V5 Input Validation | yes | FluentValidation for all DTOs; file upload validation in IFileStorageService |
| V6 Cryptography | no | No cryptographic operations in this phase |

### Known Threat Patterns

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Unauthorized report resolution | Tampering | Service checks `ReporterUserId == userId` before allowing resolve |
| Excessive photo uploads | Denial of Service | IFileStorageService enforces 5MB limit; service enforces 1-5 count |
| XSS via description/location | Tampering | Razor Views auto-encode by default; no `@Html.Raw()` usage |
| Path traversal via photo upload | Tampering | IFileStorageService uses GUID filenames, validates extensions |

## Sources

### Primary (HIGH confidence)
- Local codebase: `Pet.cs`, `AdoptionListing.cs`, `AdoptionApplication.cs`, `AdoptionService.cs`, `PetService.cs` — all read directly
- Local codebase: `AdoptionListingConfiguration.cs`, `AdoptionApplicationConfiguration.cs` — EF Core patterns
- Local codebase: `Program.cs` — DI registration pattern
- Local codebase: `FileStorageService.cs` — file upload implementation

### Secondary (MEDIUM confidence)
- 04-CONTEXT.md — all decisions from discussion phase
- 04-DISCUSSION-LOG.md — alternatives considered and rationale
- REQUIREMENTS.md — LOST-01 through LOST-03 definitions

### Tertiary (LOW confidence)
- None — all findings based on local codebase analysis and context documents

## Metadata

**Confidence breakdown:**
- Standard Stack: HIGH — all packages already in project, patterns proven in 3 prior phases
- Architecture: HIGH — direct analogue exists (Adoption module), Clean Architecture enforced
- Pitfalls: HIGH — derived from actual codebase patterns and established guard clauses

**Research date:** 2026-07-20
**Valid until:** 2026-08-20 (30 days — stable stack, no fast-moving dependencies)

## RESEARCH COMPLETE
