---
phase: 05-medical-records
fixed_at: 2026-07-21T00:30:00Z
review_path: .planning/phases/05-medical-records/05-REVIEW.md
iteration: 1
findings_in_scope: 13
fixed: 12
skipped: 1
status: partial
---

# Phase 5: Code Review Fix Report

**Fixed at:** 2026-07-21T00:30:00Z
**Source review:** .planning/phases/05-medical-records/05-REVIEW.md
**Iteration:** 1

**Summary:**
- Findings in scope: 13 (5 Critical, 8 Warning)
- Fixed: 12
- Skipped: 1

## Fixed Issues

### CR-01: EF Core LINQ Translation Failure in SearchVetsAsync

**Files modified:** `src/PetPlatform.Infrastructure/Services/VetService.cs`
**Applied fix:** Materialized `IQueryable` with `ToListAsync()` before applying in-memory `.Select(MapToProfileDto).ToList()` in both `SearchVetsAsync` (line 112) and `GetAllVetProfilesAsync` (line 287). This prevents EF Core from attempting to translate the private static `MapToProfileDto` method to SQL.

### CR-02: Admin Auto-Accept Assignment Always Fails

**Files modified:** `src/PetPlatform.Host.MVC/Areas/Admin/Controllers/VetAssignmentController.cs`
**Applied fix:** After creating an assignment, the code now looks up the vet profile's `UserId` via `_context.VetProfiles.FindAsync(vetProfileId)` and passes that to `AcceptAssignmentAsync` instead of the admin's own `userId`. This ensures the ownership check in `AcceptAssignmentAsync` (`assignment.VetProfile.UserId != vetUserId`) passes correctly.

### CR-03: IDOR — Vet Medical Record Detail Endpoints Lack Authorization

**Files modified:** `src/PetPlatform.Host.MVC/Areas/Vet/Controllers/VaccinationController.cs`, `src/PetPlatform.Host.MVC/Areas/Vet/Controllers/MedicationController.cs`, `src/PetPlatform.Host.MVC/Areas/Vet/Controllers/VisitNoteController.cs`
**Applied fix:** Added assignment verification in all three `Details` actions. After fetching the record, the code now gets the current user's `userId`, calls `GetActiveAssignmentAsync(record.PetId, userId)`, and returns `Forbid()` if no active assignment exists. This prevents any authenticated vet user from viewing medical records for pets they are not assigned to.

### CR-04: VetService.UpdateAvailabilityAsync — No Duplicate Day Validation

**Files modified:** `src/PetPlatform.Infrastructure/Services/VetService.cs`
**Applied fix:** Added a duplicate day check before persisting: `schedule.GroupBy(e => e.DayOfWeek).Where(g => g.Count() > 1)` detects duplicates and returns a `Result<bool>.Failure(...)` with the conflicting day names. This prevents `DbUpdateException` from the unique index on `(VetProfileId, DayOfWeek)`.

### CR-05: VetService.CreateProfileAsync Ignores userId Parameter

**Files modified:** `src/PetPlatform.Infrastructure/Services/VetService.cs`
**Applied fix:** Changed `VetProfile.Create(dto.UserId, ...)` to `VetProfile.Create(userId, ...)` on line 56. The service now enforces the invariant that the authenticated `userId` parameter is used, regardless of what `dto.UserId` contains.

### WR-01: Domain Entity UpdateDetails Methods Missing Guard Clauses

**Files modified:** `src/PetPlatform.Domain/Entities/VaccinationRecord.cs`, `src/PetPlatform.Domain/Entities/MedicationRecord.cs`, `src/PetPlatform.Domain/Entities/VetVisitNote.cs`
**Applied fix:** Added `Guard.Against.NullOrWhiteSpace` validation to all `UpdateDetails` methods, matching the same guards used in the corresponding `Create` methods:
- `VaccinationRecord.UpdateDetails`: validates `vaccineName`
- `MedicationRecord.UpdateDetails`: validates `medicationName`, `dosage`, `frequency`
- `VetVisitNote.UpdateDetails`: validates `subjective`, `objective`, `assessment`, `plan`

### WR-02: VetManagementController.Details Loads All Profiles to Find One

**Files modified:** `src/PetPlatform.Host.MVC/Areas/Admin/Controllers/VetManagementController.cs`
**Applied fix:** Replaced `GetAllVetProfilesAsync(1, 1000, null).Items.FirstOrDefault(...)` with a direct `_context.VetProfiles.FindAsync(id)` query by primary key. This is both efficient and correct regardless of total profile count.

### WR-03: VetManagementController.MedicalRecords Loads All Records Into Memory

**Files modified:** `src/PetPlatform.Host.MVC/Areas/Admin/Controllers/VetManagementController.cs`
**Applied fix:** Added `petId` filter at the DB level (`Where` clause) before materialization, and added `Take(page * pageSize)` to each query to limit the number of records fetched. Previously all records of each type were loaded without any DB-level filtering.

### WR-05: N+1 Query Pattern in MedicalRecordService Mapper Methods

**Files modified:** `src/PetPlatform.Infrastructure/Services/MedicalRecordService.cs`
**Applied fix:** Refactored `GetVaccinationsByPetIdAsync`, `GetMedicationsByPetIdAsync`, and `GetVisitNotesByPetIdAsync` to batch-load all needed vet profiles in a single query using `ToDictionaryAsync`, then map records using new synchronous `MapToVaccinationDto(record, vetFullName)` overloads that accept a pre-resolved vet name. This eliminates the N+1 query pattern where each record triggered a separate DB round-trip.

### WR-06: ProfileController.Edit Redundant Ownership Check

**Files modified:** `src/PetPlatform.Host.MVC/Areas/Vet/Controllers/ProfileController.cs`
**Applied fix:** Removed the dead-code ownership check `if (profile.UserId != userId)` at line 95-98 in the `Edit` GET action. The profile was already fetched via `GetProfileByUserIdAsync(userId)`, making this check always false.

### WR-07: AssignmentController.Details Loads All Assignments to Find One

**Files modified:** `src/PetPlatform.Application/Interfaces/IVetService.cs`, `src/PetPlatform.Infrastructure/Services/VetService.cs`, `src/PetPlatform.Host.MVC/Areas/Vet/Controllers/AssignmentController.cs`
**Applied fix:** Added a new `GetAssignmentByIdAsync(int assignmentId, string vetUserId)` method to `IVetService` and `VetService` that queries by primary key with an ownership check (`va.VetProfile.UserId == vetUserId`). Updated `AssignmentController.Details` to use this single-query method instead of loading all pending and accepted assignments and concatenating in memory.

### WR-08: VetProfileConfiguration Allows Duplicate UserId Before EF Save

**Files modified:** `src/PetPlatform.Infrastructure/Services/VetService.cs`
**Applied fix:** Wrapped the `SaveChangesAsync` call in `CreateProfileAsync` with a `try-catch` for `DbUpdateException`. This catches the unique constraint violation on the `UserId` index and returns a user-friendly error message instead of an unhandled 500 error.

## Skipped Issues

### WR-04: Controllers Inject IApplicationDbContext Directly Bypassing Service Layer

**File:** `src/PetPlatform.Host.MVC/Areas/Customer/Controllers/MedicalHistoryController.cs:15`, `src/PetPlatform.Host.MVC/Areas/Customer/Controllers/VetDiscoveryController.cs:16`, `src/PetPlatform.Host.MVC/Areas/Admin/Controllers/VetManagementController.cs:18`, `src/PetPlatform.Host.MVC/Areas/Admin/Controllers/VetAssignmentController.cs:17`
**Reason:** Requires broader architectural refactoring — adding new methods to `IVetService`/`IMedicalRecordService` interfaces, implementing them, updating DI registration, and removing `IApplicationDbContext` from 4 controller constructors. The `VetDiscoveryController` uses `_context` for 4 different entity types across multiple actions, and `VetAssignmentController` uses it for dropdown population. This is a systemic change that should be planned as a separate refactoring phase.
**Original issue:** Controllers inject `IApplicationDbContext` and query it directly, bypassing the service layer and violating Clean Architecture dependency rules.

---

_Fixed: 2026-07-21T00:30:00Z_
_Fixer: the agent (gsd-code-fixer)_
_Iteration: 1_
