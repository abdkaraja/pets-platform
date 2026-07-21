---
phase: 04-lost-pets
reviewed: 2026-07-21T00:00:00Z
depth: standard
files_reviewed: 6
files_reviewed_list:
  - src/PetPlatform.Domain/Entities/LostPetReport.cs
  - src/PetPlatform.Domain/Entities/LostPetReportPhoto.cs
  - src/PetPlatform.Domain/Entities/MatchNotification.cs
  - src/PetPlatform.Infrastructure/Services/LostPetService.cs
  - src/PetPlatform.Host.MVC/Controllers/LostPetController.cs
  - src/PetPlatform.Host.MVC/Areas/Customer/Controllers/LostPetController.cs
findings:
  critical: 5
  warning: 3
  info: 3
  total: 11
status: issues_found
---

# Phase 4: Code Review Report

**Reviewed:** 2026-07-21T00:00:00Z
**Depth:** standard
**Files Reviewed:** 6
**Status:** issues_found

## Summary

The domain entities (`LostPetReport`, `LostPetReportPhoto`, `MatchNotification`) follow a clean DDD pattern with private constructors and public static factory methods. However, the `LostPetService` infrastructure implementation has **five critical defects** — it bypasses the factory methods by calling non-existent constructors, references a non-existent method name, and silently loses match notification data. These issues are compilation errors that indicate the service was written against a different version of the domain entities. Additionally, match notifications are added to the DbContext but never saved, resulting in silent data loss.

## Critical Issues

### CR-01: ~~LostPetService calls non-existent constructor on LostPetReport~~ ✅ FIXED

**File:** `src/PetPlatform.Infrastructure/Services/LostPetService.cs:90-99`
**Issue:** `CreateReportAsync` calls `new LostPetReport(reporterUserId: ..., reportType: ..., ...)` using a constructor-with-parameters pattern. The entity `LostPetReport` only has a `private LostPetReport() { }` parameterless constructor (for EF Core). The public creation API is the static factory `LostPetReport.Create(...)`. This is a compilation error — the code will not build.
**Fix:** Replaced `new LostPetReport(...)` with `LostPetReport.Create(...)` using the correct factory method signature.

### CR-02: ~~LostPetService calls non-existent constructor on LostPetReportPhoto~~ ✅ FIXED

**File:** `src/PetPlatform.Infrastructure/Services/LostPetService.cs:109, 140`
**Issue:** Both `CreateReportAsync` and `UpdateReportAsync` call `new LostPetReportPhoto(report.Id, photoPath)`. The entity only has a `private LostPetReportPhoto() { }` parameterless constructor. The public creation API is `LostPetReportPhoto.Create(int lostPetReportId, string photoPath)`. This is a compilation error in two locations.
**Fix:** Replaced both instances of `new LostPetReportPhoto(...)` with `LostPetReportPhoto.Create(...)`.

### CR-03: ~~LostPetService calls non-existent constructor on MatchNotification~~ ✅ FIXED

**File:** `src/PetPlatform.Infrastructure/Services/LostPetService.cs:233-234`
**Issue:** `CheckForMatchesAsync` calls `new MatchNotification(newReport.Id, candidate.Id, ...)` in two places. The entity only has a `private MatchNotification() { }` parameterless constructor. The public creation API is `MatchNotification.Create(int matchedReportId, int triggeredReportId, string reporterUserId, string message)`. This is a compilation error.
**Fix:** Replaced both `new MatchNotification(...)` calls with `MatchNotification.Create(...)` using the correct factory method signature.

### CR-04: ~~LostPetService calls report.Update() but entity defines UpdateDetails()~~ ✅ FIXED

**File:** `src/PetPlatform.Infrastructure/Services/LostPetService.cs:128`
**Issue:** `UpdateReportAsync` calls `report.Update(color: ..., breed: ..., ...)` but the entity method is named `UpdateDetails(...)`. There is no `Update` method on `LostPetReport`. This is a compilation error — the method name does not exist on the entity.
**Fix:** Changed `report.Update(...)` to `report.UpdateDetails(...)` with the correct method name.

### CR-05: ~~Match notifications silently lost — missing SaveChangesAsync after CheckForMatchesAsync~~ ✅ FIXED

**File:** `src/PetPlatform.Infrastructure/Services/LostPetService.cs:115-117`
**Issue:** `CreateReportAsync` calls `CheckForMatchesAsync(report)` which adds `MatchNotification` entities to the DbContext via `_context.MatchNotifications.AddRange(...)`. However, `CreateReportAsync` returns immediately after without calling `SaveChangesAsync()`. The match notifications are tracked by EF Core but never persisted to the database. Users will never receive match notifications when reports are created. This is silent data loss.
**Fix:** Added `await _context.SaveChangesAsync();` after `CheckForMatchesAsync(report)` to persist match notifications to the database.

## Warnings

### WR-01: ~~N+1 query pattern in CheckForMatchesAsync~~ ✅ FIXED

**File:** `src/PetPlatform.Infrastructure/Services/LostPetService.cs:223-236`
**Issue:** Inside the `foreach (var candidate in candidates)` loop, a separate `AnyAsync` query is executed per candidate to check for duplicate notifications (lines 225-228). If there are N candidates, this results in N additional database queries. This should be batched into a single query.
**Fix:** Replaced N+1 per-candidate `AnyAsync` with a single batched query that fetches all existing notification pairs for candidate IDs upfront, builds a `HashSet` of known pairs (including both directions), and checks membership in the loop without additional DB calls.

### WR-02: ~~No file upload validation in controllers~~ ✅ FIXED

**File:** `src/PetPlatform.Host.MVC/Areas/Customer/Controllers/LostPetController.cs:41, 100`
**Issue:** The `Create` and `Edit` POST actions accept `List<IFormFile> photos` without any validation on file type, file size, or number of files. A user could upload arbitrarily large files, an unlimited number of files, or files with dangerous extensions (e.g., `.exe`, `.html` for stored XSS). While `IFileStorageService` may have some guards, the controller layer should enforce limits before passing files downstream.
**Fix:** Added a `ValidatePhotos` helper method that checks allowed extensions (.jpg, .jpeg, .png, .gif, .webp), max file size (5 MB), and max file count (5). Called in both `Create` and `Edit` POST actions before passing files to the service.

### WR-03: ~~SearchReportsAsync vulnerable to negative Skip value~~ ✅ FIXED

**File:** `src/PetPlatform.Infrastructure/Services/LostPetService.cs:65`
**Issue:** `Skip((filter.Page - 1) * filter.PageSize)` will produce a negative value if `filter.Page` is 0 or negative. While the DTO defaults `Page` to 1, there is no server-side validation or guard clause preventing `Page <= 0` from being passed in. This will cause an `InvalidOperationException` from EF Core.
**Fix:** Added guard clauses at the start of `SearchReportsAsync`: `filter.Page = Math.Max(1, filter.Page)` and `filter.PageSize = Math.Clamp(filter.PageSize, 1, 100)` to ensure valid pagination values.

## Info

### IN-01: Inconsistent case handling in breed filter vs SearchTerm filter

**File:** `src/PetPlatform.Infrastructure/Services/LostPetService.cs:37 vs 57`
**Issue:** The breed filter (line 37) uses `r.Breed.Contains(filter.Breed)` which is case-sensitive in C# (case-insensitive only if the DB collation is case-insensitive). However, the SearchTerm filter (line 57) explicitly calls `.ToLower()` on both sides. This inconsistency may produce different results depending on the database provider.
**Fix:** Apply consistent case handling — either use `ToLower()` / `ToUpperInvariant()` on the breed filter or rely on DB-level case-insensitive collation for both:
```csharp
// Line 37 — make consistent with SearchTerm:
query = query.Where(r => r.Breed != null && r.Breed.ToLower().Contains(filter.Breed.ToLower()));
```

### IN-02: MatchNotification.MarkAsRead() does not track when it was marked

**File:** `src/PetPlatform.Domain/Entities/MatchNotification.cs:42-45`
**Issue:** `MarkAsRead()` sets `IsRead = true` but does not record a timestamp. The entity has no `UpdatedAt` property, so there is no way to know when the notification was marked as read. For audit trails and UX (e.g., "marked as read 2 hours ago"), this is useful information.
**Fix:** Add an `UpdatedAt` property and set it in `MarkAsRead()`:
```csharp
public DateTime? UpdatedAt { get; private set; }

public void MarkAsRead()
{
    IsRead = true;
    UpdatedAt = DateTime.UtcNow;
}
```

### IN-03: ViewBag.ExistingPhotos used instead of strongly-typed ViewModel

**File:** `src/PetPlatform.Host.MVC/Areas/Customer/Controllers/LostPetController.cs:94`
**Issue:** `ViewBag.ExistingPhotos = report.Photos` passes data through an untyped dynamic bag. This provides no compile-time safety and no IntelliSense support in the view. If the property name is changed or the type changes, errors will only appear at runtime.
**Fix:** Use a strongly-typed ViewModel that includes an `ExistingPhotos` property, or use `ViewData["ExistingPhotos"]` with an explicit cast in the view.

---

_Reviewed: 2026-07-21T00:00:00Z_
_Reviewer: the agent (gsd-code-reviewer)_
_Depth: standard_
