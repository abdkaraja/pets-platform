---
phase: 03-adoption-module
reviewed: 2026-07-21T00:00:00Z
depth: standard
files_reviewed: 7
files_reviewed_list:
  - src/PetPlatform.Domain/Entities/AdoptionListing.cs
  - src/PetPlatform.Domain/Entities/AdoptionApplication.cs
  - src/PetPlatform.Domain/Entities/ApplicationStatusHistory.cs
  - src/PetPlatform.Application/Services/AdoptionService.cs
  - src/PetPlatform.Host.MVC/Controllers/AdoptionController.cs
  - src/PetPlatform.Host.MVC/Areas/Customer/Controllers/AdoptionController.cs
  - src/PetPlatform.Host.MVC/Areas/ServiceProvider/Controllers/AdoptionController.cs
findings:
  critical: 4
  warning: 5
  info: 2
  total: 11
status: all_fixed
---

# Phase 3: Code Review Report — Adoption Module

**Reviewed:** 2026-07-21T00:00:00Z
**Depth:** standard
**Files Reviewed:** 7
**Status:** all_fixed

## Summary

The adoption module implements listing creation, application submission, review workflows, and role-based controllers. The domain model uses a state machine pattern for both listing and application status transitions. **Four critical issues were found:** data loss from silently dropped DTO fields, race conditions in duplicate-check logic, an authorization gap allowing shelters to withdraw applicant submissions, and a broken domain state machine that permits invalid listing transitions. These must be addressed before the module ships.

---

## Critical Issues

### CR-01: CreateApplicationDto fields silently dropped — application data never persisted [FIXED]

**File:** `src/PetPlatform.Application/Services/AdoptionService.cs:112`
**Also:** `src/PetPlatform.Domain/Entities/AdoptionApplication.cs:32-48`

**Issue:** `CreateApplicationDto` includes seven fields — `HousingType`, `HasYard`, `NumberOfOccupants`, `HasChildren`, `PreviousPets`, `CurrentPets`, `ExperienceLevel` — that are collected from the user but never passed to `AdoptionApplication.Create()`. The entity constructor only accepts `listingId`, `applicantUserId`, and `message`. The data is silently discarded on save. The form appears to work but loses all detailed housing/experience information.

**Fix:** Either add matching properties to `AdoptionApplication` and pass them through in `Create()`, or remove the unused DTO fields if they are not needed yet. If they are needed, the entity needs:

```csharp
// In AdoptionApplication.cs
public HousingType HousingType { get; private set; }
public bool HasYard { get; private set; }
public int NumberOfOccupants { get; private set; }
public bool HasChildren { get; private set; }
public string? PreviousPets { get; private set; }
public string? CurrentPets { get; private set; }
public string? ExperienceLevel { get; private set; }

// Update Create() signature to accept these
```

---

### CR-02: Race condition allows duplicate applications — no DB-level uniqueness enforcement [FIXED]

**File:** `src/PetPlatform.Application/Services/AdoptionService.cs:106-114`
**Also:** `src/PetPlatform.Infrastructure/Persistence/Configurations/AdoptionApplicationConfiguration.cs:35`

**Issue:** `SubmitApplicationAsync` checks for an existing application (line 106-107) and then inserts a new one (lines 112-114) in two separate operations that are not atomic. Two concurrent requests from the same user can both pass the duplicate check and both create applications. The composite index on `(ListingId, ApplicantUserId)` (Configuration line 35) is **not** marked `IsUnique`, so the database does not prevent duplicates.

**Fix:** Add a unique constraint to the composite index:
```csharp
// In AdoptionApplicationConfiguration.cs line 35
builder.HasIndex(aa => new { aa.ListingId, aa.ApplicantUserId }).IsUnique();
```

And/or use a database-level lock or serializable transaction around the check-and-insert in the service.

---

### CR-03: Shelters can withdraw applications on behalf of applicants — authorization gap [FIXED]

**File:** `src/PetPlatform.Application/Services/AdoptionService.cs:299-316`

**Issue:** `ReviewApplicationAsync` accepts any `ApplicationStatus` value via `ReviewApplicationDto.Status`, including `Withdrawn`. When a shelter sends `Status = Withdrawn` for an application in `Submitted` or `UnderReview` state, the domain's `AllowedTransitions` permits the transition (`Submitted→Withdrawn` and `UnderReview→Withdrawn` are both allowed). The service method at line 304 only special-cases `UnderReview` status — all other statuses (including `Withdrawn`) fall into the `Review()` path at line 310. This means a shelter can force-withdraw an applicant's submission, which should be an applicant-only action.

**Fix:** Restrict the statuses a shelter can set. Only `UnderReview`, `Approved`, and `Rejected` should be valid for the shelter review action:

```csharp
// In ReviewApplicationAsync, after validation
var allowedShelterStatuses = new[] { ApplicationStatus.UnderReview, ApplicationStatus.Approved, ApplicationStatus.Rejected };
if (!allowedShelterStatuses.Contains(dto.Status))
    return Result<AdoptionApplicationDto>.Failure("Invalid review status.");
```

---

### CR-04: AdoptionListing.UpdateStatus state machine is broken — allows invalid transitions [FIXED]

**File:** `src/PetPlatform.Domain/Entities/AdoptionListing.cs:53-63`

**Issue:** The `UpdateStatus` method only guards transitions TO `Active` (must come from `Pending`), but does **not** validate other transitions. The `Close()` method (line 65-68) bypasses the state machine entirely. This allows:

- `Active → Pending` (regression — should not be possible)
- `Active → Adopted` (bypasses application approval flow)
- `Closed → Adopted` (nonsensical — already closed)
- `Closed → Pending` (reopening closed listing without clear intent)
- `Close()` called from any state including `Adopted` (bypasses "adopted is immutable" rule)

**Fix:** Replace the loose guard with an explicit transition map, similar to the pattern used in `AdoptionApplication`:

```csharp
private static readonly Dictionary<ListingStatus, HashSet<ListingStatus>> AllowedTransitions = new()
{
    [ListingStatus.Active] = new() { ListingStatus.Closed, ListingStatus.Adopted },
    [ListingStatus.Pending] = new() { ListingStatus.Active, ListingStatus.Closed },
    [ListingStatus.Adopted] = new(),
    [ListingStatus.Closed] = new()
};

public void UpdateStatus(ListingStatus newStatus)
{
    if (!AllowedTransitions.TryGetValue(Status, out var allowed) || !allowed.Contains(newStatus))
        throw new InvalidOperationException($"Cannot transition from {Status} to {newStatus}.");

    Status = newStatus;
    UpdatedAt = DateTime.UtcNow;
}
```

Remove the `Close()` method entirely — use `UpdateStatus(ListingStatus.Closed)` instead.

---

## Warnings

### WR-01: ApplicationDetails and ReviewApplication endpoints fetch entire collections to find one record [FIXED]

**File:** `src/PetPlatform.Host.MVC/Areas/Customer/Controllers/AdoptionController.cs:75-76`
**Also:** `src/PetPlatform.Host.MVC/Areas/ServiceProvider/Controllers/AdoptionController.cs:130-131, 152-153`

**Issue:** `ApplicationDetails` calls `GetMyApplicationsAsync(userId)` which loads all user applications with full `Include` chains (Listing, Pet, StatusHistory), then filters client-side with `.FirstOrDefault(a => a.Id == id)`. The `ReviewApplication` GET and POST actions do the same with `GetApplicationsForListingAsync`. This loads far more data than necessary.

**Fix:** Add targeted query methods to `IAdoptionService`:
```csharp
Task<AdoptionApplicationDto?> GetApplicationByIdAsync(int applicationId, string userId);
Task<AdoptionApplicationDto?> GetApplicationForReviewAsync(int applicationId, string shelterUserId);
```

---

### WR-02: `size` query parameter accepted but silently discarded [FIXED]

**File:** `src/PetPlatform.Host.MVC/Controllers/AdoptionController.cs:22`

**Issue:** The public `Index` action accepts a `string? size` parameter (line 22) but never passes it to the filter or uses it anywhere. `AdoptionListingFilterDto` has no `Size` property. Users may think their size filter is being applied.

**Fix:** Either remove the parameter, or add a `Size` property to `AdoptionListingFilterDto` and wire it through to the query.

---

### WR-03: No PageSize upper bound — user can request arbitrarily large pages [FIXED]

**File:** `src/PetPlatform.Application/DTOs/AdoptionDtos.cs:14`

**Issue:** `PageSize` defaults to 12 but has no maximum validation. A request with `PageSize=999999` would load all records in a single page. The `AdoptionListingFilterDto` is model-bound from query parameters with no FluentValidation rules for `Page` or `PageSize`.

**Fix:** Add validation (either in a FluentValidation rule or in the service layer):
```csharp
// In service or validator
filter.PageSize = Math.Clamp(filter.PageSize, 1, 50);
filter.Page = Math.Max(1, filter.Page);
```

---

### WR-04: Page=0 or negative page values cause exception from negative Skip argument [FIXED]

**File:** `src/PetPlatform.Application/Services/AdoptionService.cs:64`

**Issue:** `Skip((filter.Page - 1) * filter.PageSize)` with `Page=0` computes `Skip(-12)`, which throws an `InvalidOperationException` in most EF Core providers. While the public controller defaults `page = 1`, the service method accepts any value, and internal callers or API consumers could pass `Page=0`.

**Fix:** Clamp the page value before use:
```csharp
var page = Math.Max(1, filter.Page);
var items = await query
    .OrderByDescending(al => al.CreatedAt)
    .Skip((page - 1) * filter.PageSize)
    .Take(filter.PageSize)
    .Select(al => MapToListingDto(al))
    .ToListAsync();
```

---

### WR-05: `MapToApplicationDtoAsync` is needlessly async — wraps synchronous call [FIXED]

**File:** `src/PetPlatform.Application/Services/AdoptionService.cs:373-376`

**Issue:** `MapToApplicationDtoAsync` is declared `async Task<AdoptionApplicationDto>` but its body simply returns `MapToApplicationDto(application)` — a synchronous method. There is no `await` inside. This generates a compiler warning and misleads readers into expecting async I/O.

**Fix:** Either remove the method and call `MapToApplicationDto` directly (changing `SubmitApplicationAsync` line 116 from `await MapToApplicationDtoAsync(...)` to just `MapToApplicationDto(...)`), or inline the mapping.

---

## Info

### IN-01: `GetApplicationsForListingAsync` returns empty for both "not found" and "unauthorized"

**File:** `src/PetPlatform.Application/Services/AdoptionService.cs:260-265`

**Issue:** When the listing doesn't exist (line 262) or the user doesn't own it (line 265), the method returns `Enumerable.Empty<AdoptionApplicationDto>()`. Callers cannot distinguish between "listing not found" and "you don't have access." The ServiceProvider controller at line 131 uses this to show `NotFound()`, but that's only correct for the first case.

**Fix:** Return `Result<IEnumerable<AdoptionApplicationDto>>` with appropriate error messages, or split into two checks in the controller.

---

### IN-02: `AdoptionApplication.Create` records StatusHistory with `ApplicationId = 0`

**File:** `src/PetPlatform.Domain/Entities/AdoptionApplication.cs:46`

**Issue:** `ApplicationStatusHistory.Create(application.Id, ...)` is called before the application is persisted, so `application.Id` is 0. This works because EF Core fixes up FK values in the tracked entity graph on `SaveChangesAsync`, but it is fragile and non-obvious. A future refactor that decouples the history recording from the entity constructor would silently produce broken records.

**Fix:** Consider recording the initial status history in the service layer after the entity is saved, or document this EF Core dependency clearly.

---

_Reviewed: 2026-07-21T00:00:00Z_
_Reviewer: the agent (gsd-code-reviewer)_
_Depth: standard_
