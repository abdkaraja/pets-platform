---
phase: 05-medical-records
reviewed: 2026-07-21T00:00:00Z
depth: standard
files_reviewed: 33
files_reviewed_list:
  - src/PetPlatform.Domain/Enums/VetAssignmentStatus.cs
  - src/PetPlatform.Domain/Enums/MedicalRecordType.cs
  - src/PetPlatform.Domain/Entities/VetProfile.cs
  - src/PetPlatform.Domain/Entities/VetAvailability.cs
  - src/PetPlatform.Domain/Entities/VetAssignment.cs
  - src/PetPlatform.Domain/Entities/VaccinationRecord.cs
  - src/PetPlatform.Domain/Entities/MedicationRecord.cs
  - src/PetPlatform.Domain/Entities/VetVisitNote.cs
  - src/PetPlatform.Infrastructure/Persistence/Configurations/VetProfileConfiguration.cs
  - src/PetPlatform.Infrastructure/Persistence/Configurations/VetAvailabilityConfiguration.cs
  - src/PetPlatform.Infrastructure/Persistence/Configurations/VetAssignmentConfiguration.cs
  - src/PetPlatform.Infrastructure/Persistence/Configurations/VaccinationRecordConfiguration.cs
  - src/PetPlatform.Infrastructure/Persistence/Configurations/MedicationRecordConfiguration.cs
  - src/PetPlatform.Infrastructure/Persistence/Configurations/VetVisitNoteConfiguration.cs
  - src/PetPlatform.Application/DTOs/MedicalDtos.cs
  - src/PetPlatform.Application/Interfaces/IVetService.cs
  - src/PetPlatform.Application/Interfaces/IMedicalRecordService.cs
  - src/PetPlatform.Application/Validators/CreateVaccinationValidator.cs
  - src/PetPlatform.Application/Validators/CreateMedicationValidator.cs
  - src/PetPlatform.Application/Validators/CreateVetVisitNoteValidator.cs
  - src/PetPlatform.Application/Validators/CreateVetProfileValidator.cs
  - src/PetPlatform.Infrastructure/Services/VetService.cs
  - src/PetPlatform.Infrastructure/Services/MedicalRecordService.cs
  - src/PetPlatform.Host.MVC/Areas/Vet/Controllers/DashboardController.cs
  - src/PetPlatform.Host.MVC/Areas/Vet/Controllers/ProfileController.cs
  - src/PetPlatform.Host.MVC/Areas/Vet/Controllers/PetsController.cs
  - src/PetPlatform.Host.MVC/Areas/Vet/Controllers/AssignmentController.cs
  - src/PetPlatform.Host.MVC/Areas/Vet/Controllers/VaccinationController.cs
  - src/PetPlatform.Host.MVC/Areas/Vet/Controllers/MedicationController.cs
  - src/PetPlatform.Host.MVC/Areas/Vet/Controllers/VisitNoteController.cs
  - src/PetPlatform.Host.MVC/Areas/Customer/Controllers/MedicalHistoryController.cs
  - src/PetPlatform.Host.MVC/Areas/Customer/Controllers/VetDiscoveryController.cs
  - src/PetPlatform.Host.MVC/Areas/Admin/Controllers/VetManagementController.cs
  - src/PetPlatform.Host.MVC/Areas/Admin/Controllers/VetAssignmentController.cs
findings:
  critical: 5
  warning: 8
  info: 3
  total: 16
status: issues_found
---

# Phase 5: Code Review Report

**Reviewed:** 2026-07-21T00:00:00Z
**Depth:** standard
**Files Reviewed:** 33
**Status:** issues_found

## Summary

This phase implements the veterinary medical records system: domain entities (VetProfile, VetAvailability, VetAssignment, VaccinationRecord, MedicationRecord, VetVisitNote), EF Core configurations, FluentValidation validators, service implementations, and MVC controllers across Vet/Customer/Admin areas. The domain model and validation logic are well-structured. However, the review uncovered 5 critical bugs including EF LINQ translation failures, a logic error in admin assignment auto-accept, and IDOR vulnerabilities on medical record detail endpoints. There are also 8 warnings around missing validation, architectural violations, and scalability concerns.

## Critical Issues

### CR-01: EF Core LINQ Translation Failure in SearchVetsAsync

**File:** `src/PetPlatform.Infrastructure/Services/VetService.cs:116`
**Issue:** The `.Select(vp => MapToProfileDto(vp))` call is inside an `IQueryable` chain. EF Core cannot translate the private static `MapToProfileDto` method to SQL. This will throw `InvalidOperationException` at runtime when a vet user performs a search. The same issue exists in `GetAllVetProfilesAsync` at line 291.
**Fix:**
```csharp
// Line 112-117 — materialize first, then project in-memory
var items = (await query
    .OrderByDescending(vp => vp.CreatedAt)
    .Skip((filter.Page - 1) * filter.PageSize)
    .Take(filter.PageSize)
    .ToListAsync())
    .Select(MapToProfileDto)
    .ToList();
```
Apply the same fix at line 287-292 in `GetAllVetProfilesAsync`.

### CR-02: Admin Auto-Accept Assignment Always Fails

**File:** `src/PetPlatform.Host.MVC/Areas/Admin/Controllers/VetAssignmentController.cs:103`
**Issue:** After creating an assignment via `RequestAssignmentAsync`, the code attempts to auto-accept it by calling `AcceptAssignmentAsync(result.Value.Id, userId)` where `userId` is the admin's user ID. However, `AcceptAssignmentAsync` (VetService.cs:181) checks `assignment.VetProfile.UserId != vetUserId` — comparing the admin's ID against the vet's user ID. This will ALWAYS fail unless the admin happens to be the vet themselves. Admin-created assignments will remain permanently in `Pending` status.
**Fix:**
```csharp
// Pass the vet's UserId instead of the admin's userId
var vetProfile = await _context.VetProfiles.FindAsync(vetProfileId);
if (vetProfile != null)
{
    var acceptResult = await _vetService.AcceptAssignmentAsync(result.Value.Id, vetProfile.UserId);
    // ...
}
```
Or add a dedicated `ForceAcceptAsync(int assignmentId)` method to `IVetService` that skips the user ownership check for admin operations.

### CR-03: IDOR — Vet Medical Record Detail Endpoints Lack Authorization

**File:** `src/PetPlatform.Host.MVC/Areas/Vet/Controllers/VaccinationController.cs:78`
**File:** `src/PetPlatform.Host.MVC/Areas/Vet/Controllers/MedicationController.cs:78`
**File:** `src/PetPlatform.Host.MVC/Areas/Vet/Controllers/VisitNoteController.cs:78`
**Issue:** The `Details` action on all three medical record controllers fetches a record by ID and returns it to the view without verifying that the current vet has an active assignment for the associated pet. Any authenticated Vet user can enumerate and view any medical record (vaccination, medication, or visit note) by iterating IDs. This is an Insecure Direct Object Reference (IDOR) vulnerability exposing pet medical data.
**Fix:** Add authorization check in each Details method:
```csharp
public async Task<IActionResult> Details(int id)
{
    var record = await _medicalRecordService.GetVaccinationByIdAsync(id);
    if (record == null) return NotFound();

    var userId = _userManager.GetUserId(User);
    if (string.IsNullOrEmpty(userId)) return Challenge();

    var assignment = await _vetService.GetActiveAssignmentAsync(record.PetId, userId);
    if (assignment == null) return Forbid();

    ViewData["Title"] = $"Vaccination — {record.VaccineName}";
    return View(record);
}
```
Apply the same pattern to `MedicationController.Details` and `VisitNoteController.Details`.

### CR-04: VetService.UpdateAvailabilityAsync — No Duplicate Day Validation Causes Unhandled DB Exception

**File:** `src/PetPlatform.Infrastructure/Services/VetService.cs:361`
**Issue:** The `UpdateAvailabilityAsync` method removes all existing availability entries and adds new ones from the `schedule` list. The `VetAvailabilityConfiguration` defines a unique index on `(VetProfileId, DayOfWeek)`. If the incoming schedule contains two entries for the same `DayOfWeek`, the `SaveChangesAsync` call will throw a `DbUpdateException` (unique constraint violation) that is not caught. This will result in an unhandled 500 error.
**Fix:**
```csharp
// Add duplicate check before persisting
var duplicateDays = schedule
    .GroupBy(e => e.DayOfWeek)
    .Where(g => g.Count() > 1)
    .Select(g => g.Key);
if (duplicateDays.Any())
    return Result<bool>.Failure($"Duplicate availability entries for: {string.Join(", ", duplicateDays)}");
```

### CR-05: VetService.CreateProfileAsync Ignores userId Parameter in Favor of dto.UserId

**File:** `src/PetPlatform.Infrastructure/Services/VetService.cs:56`
**Issue:** The method signature accepts `string userId` and validates it with a guard clause (line 41), but the actual profile creation on line 56 uses `dto.UserId` instead of the `userId` parameter: `VetProfile.Create(dto.UserId, ...)`. If a malicious caller (e.g., from an API endpoint or future code) passes a `dto.UserId` that differs from the authenticated `userId`, a profile would be created for a different user. The controller currently overwrites `dto.UserId = userId` before calling this, but the service itself does not enforce the invariant.
**Fix:**
```csharp
// Line 56 — use the authenticated userId, not dto.UserId
var profile = VetProfile.Create(userId, dto.FullName, dto.Clinic, dto.Specialty, dto.Bio, dto.ServicesOffered);
```

## Warnings

### WR-01: Domain Entity UpdateDetails Methods Missing Guard Clauses

**File:** `src/PetPlatform.Domain/Entities/VaccinationRecord.cs:49`
**File:** `src/PetPlatform.Domain/Entities/MedicationRecord.cs:60`
**File:** `src/PetPlatform.Domain/Entities/VetVisitNote.cs:55`
**Issue:** The `Create` methods on these entities use `Guard.Against.NullOrWhiteSpace` for required string parameters, but the corresponding `UpdateDetails` methods do not validate inputs at all. This allows empty or whitespace strings to overwrite valid data via the update path, bypassing the domain invariants established during creation.
**Fix:** Add the same guard clauses from `Create` to `UpdateDetails`:
```csharp
// VaccinationRecord.UpdateDetails
public void UpdateDetails(string vaccineName, ...)
{
    Guard.Against.NullOrWhiteSpace(vaccineName, nameof(vaccineName));
    // ... rest of method
}
```
Apply similarly to `MedicationRecord.UpdateDetails` (validate `medicationName`, `dosage`, `frequency`) and `VetVisitNote.UpdateDetails` (validate `subjective`, `objective`, `assessment`, `plan`).

### WR-02: VetManagementController.Details Loads All Profiles to Find One

**File:** `src/PetPlatform.Host.MVC/Areas/Admin/Controllers/VetManagementController.cs:44`
**Issue:** The `Details` action calls `GetAllVetProfilesAsync(1, 1000, null)` to load up to 1000 vet profiles into memory, then filters with `.FirstOrDefault(vp => vp.Id == id)`. This is both inefficient and fragile — if there are more than 1000 profiles, the target profile may not be found.
**Fix:** Add a `GetVetProfileByIdAsync(int id)` method to `IVetService` that queries by primary key directly, or use `_context.VetProfiles.FindAsync(id)` directly in the controller.

### WR-03: VetManagementController.MedicalRecords Loads All Records Into Memory

**File:** `src/PetPlatform.Host.MVC/Areas/Admin/Controllers/VetManagementController.cs:101-114`
**Issue:** This action loads ALL vaccination records, ALL medication records, and ALL visit notes from the database into memory (`ToListAsync()` with no `Where` clause), then combines them, optionally filters by `petId`, sorts, and paginates in-memory. As the data grows, this will consume increasing memory and have slow response times. With 10K+ medical records, this is a scalability risk.
**Fix:** Implement server-side filtering and pagination using a unified query or use the existing `MedicalRecordService.GetMedicalHistoryAsync` pattern with proper DB-level filtering and pagination.

### WR-04: Controllers Inject IApplicationDbContext Directly Bypassing Service Layer

**File:** `src/PetPlatform.Host.MVC/Areas/Customer/Controllers/MedicalHistoryController.cs:15`
**File:** `src/PetPlatform.Host.MVC/Areas/Customer/Controllers/VetDiscoveryController.cs:16`
**File:** `src/PetPlatform.Host.MVC/Areas/Admin/Controllers/VetManagementController.cs:18`
**File:** `src/PetPlatform.Host.MVC/Areas/Admin/Controllers/VetAssignmentController.cs:17`
**Issue:** Multiple controllers inject `IApplicationDbContext` and query it directly instead of going through application services. This violates the layered architecture (Domain → Application → Infrastructure) by creating direct coupling between the presentation layer and the persistence layer. Business logic and authorization checks in these direct queries may be inconsistent with the service layer.
**Fix:** Move all data access into service classes (`IVetService`, `IMedicalRecordService`, or new services) and have controllers call only service methods.

### WR-05: N+1 Query Pattern in MedicalRecordService Mapper Methods

**File:** `src/PetPlatform.Infrastructure/Services/MedicalRecordService.cs:318-321`
**File:** `src/PetPlatform.Infrastructure/Services/MedicalRecordService.cs:340-343`
**File:** `src/PetPlatform.Infrastructure/Services/MedicalRecordService.cs:365-368`
**Issue:** Each mapper method (`MapToVaccinationDtoAsync`, `MapToMedicationDtoAsync`, `MapToVisitNoteDtoAsync`) executes a separate DB query to look up the vet profile by `UserId`. When called in a loop (e.g., `GetVaccinationsByPetIdAsync` at lines 94-98), this creates an N+1 query pattern where N records produce N additional database round-trips.
**Fix:** For the list methods, batch-load all needed vet profiles first, then use a dictionary for mapping:
```csharp
public async Task<IEnumerable<VaccinationRecordDto>> GetVaccinationsByPetIdAsync(int petId)
{
    var records = await _context.VaccinationRecords
        .Include(vr => vr.Pet)
        .Where(vr => vr.PetId == petId)
        .OrderByDescending(vr => vr.DateAdministered)
        .ToListAsync();

    var vetUserIds = records.Select(r => r.VetUserId).Distinct().ToList();
    var vetProfiles = await _context.VetProfiles
        .Where(vp => vetUserIds.Contains(vp.UserId))
        .ToDictionaryAsync(vp => vp.UserId, vp => vp.FullName);

    return records.Select(r => MapToVaccinationDto(r, vetProfiles.GetValueOrDefault(r.VetUserId, ""))).ToList();
}
```

### WR-06: ProfileController.Edit Redundant Ownership Check

**File:** `src/PetPlatform.Host.MVC/Areas/Vet/Controllers/ProfileController.cs:95`
**Issue:** After fetching the profile via `GetProfileByUserIdAsync(userId)` (which filters by `userId`), line 95 checks `if (profile.UserId != userId)` — this condition can never be true because the profile was fetched using the same userId. This is dead code that suggests a copy-paste from a different pattern.
**Fix:** Remove lines 95-98 (the redundant check and `return NotFound()`).

### WR-07: AssignmentController.Details Loads All Assignments to Find One

**File:** `src/PetPlatform.Host.MVC/Areas/Vet/Controllers/AssignmentController.cs:41-44`
**Issue:** The `Details` action loads ALL pending and ALL accepted assignments for the current vet, concatenates them in memory, and then filters by ID with `.FirstOrDefault(a => a.Id == id)`. This fetches far more data than needed. If a vet has many assignments, this is wasteful and may miss rejected assignments entirely (which are neither pending nor accepted).
**Fix:** Add a `GetAssignmentByIdAsync(int assignmentId, string vetUserId)` method to `IVetService` that queries by primary key with an ownership check.

### WR-08: VetProfileConfiguration Allows Duplicate UserId Before Entity Framework Save

**File:** `src/PetPlatform.Infrastructure/Persistence/Configurations/VetProfileConfiguration.cs:33-34`
**Issue:** The unique index on `UserId` ensures database-level uniqueness, but `VetService.CreateProfileAsync` (line 50-54) performs a race-condition-prone check-then-insert: it queries for an existing profile and then inserts. Under concurrent requests, two profiles for the same UserId could be created simultaneously, with the second failing at `SaveChangesAsync` with an unhandled `DbUpdateException`. The service doesn't catch this.
**Fix:** Wrap the save in a try-catch for `DbUpdateException` and translate it to a user-friendly error:
```csharp
try
{
    await _context.SaveChangesAsync();
}
catch (DbUpdateException)
{
    return Result<VetProfileDto>.Failure("A vet profile already exists for this user.");
}
```

## Info

### IN-01: DashboardController N+1 Query Pattern for Recent Records

**File:** `src/PetPlatform.Host.MVC/Areas/Vet/Controllers/DashboardController.cs:43-47`
**Issue:** The loop calls `GetRecentRecordsAsync` for each accepted assignment, which internally calls `GetMedicalHistoryAsync`, loading all medical records for each pet. For a vet with many assigned pets, this creates a cascading N+1+1 problem (N pets, each triggering 3 DB queries in `GetMedicalHistoryAsync`).
**Fix:** Consider a batch method like `GetRecentRecordsForPetsAsync(IEnumerable<int> petIds)` or cache the dashboard counts.

### IN-02: PetDetailsViewModel Defined Inside Controller File

**File:** `src/PetPlatform.Host.MVC/Areas/Vet/Controllers/PetsController.cs:70-79`
**Issue:** The `PetDetailsViewModel` class is defined at the bottom of the controller file rather than in a dedicated ViewModels folder or file. This is a minor organizational concern that makes the class harder to discover and reuse.
**Fix:** Move `PetDetailsViewModel` to its own file under a `ViewModels` directory.

### IN-03: VetAvailabilityConfiguration Unique Index Allows Only One Entry Per Day

**File:** `src/PetPlatform.Infrastructure/Persistence/Configurations/VetAvailabilityConfiguration.cs:21-22`
**Issue:** The unique index `(VetProfileId, DayOfWeek)` means a vet can only have one availability window per day. If a vet works morning and evening shifts with a break in between, this model cannot represent that. This may be intentional but limits flexibility.
**Fix:** If split shifts are needed, change the unique constraint to `(VetProfileId, DayOfWeek, StartTime)` and adjust validation. If single-window-per-day is intentional, no change needed.

---

_Reviewed: 2026-07-21T00:00:00Z_
_Reviewer: the agent (gsd-code-reviewer)_
_Depth: standard_
