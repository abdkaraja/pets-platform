# Phase 5: Medical Records & Admin Expansion - Context

**Gathered:** 2026-07-21
**Status:** Ready for planning

<domain>
## Phase Boundary

Vets can record pet health data digitally (vaccination history, medication records, SOAP visit notes) and pet owners can view their pet's complete medical history. Admin tools expand to manage vet profiles and assignments. The vet dashboard is a full area with sidebar, stats, and calendar.

</domain>

<decisions>
## Implementation Decisions

### Vet-Pet Assignment
- **D-01:** Owner requests a vet — two-step workflow. Owner selects a vet from a searchable list, sends assignment request. Vet accepts or rejects.
- **D-02:** Vet discovery via search/filter — owner can search vets by name, specialty, location. Vets have profiles with: name, clinic, specialty, bio, services offered, availability schedule.
- **D-03:** Assignment statuses: Pending, Accepted, Rejected. Vet can see pending requests and accept/reject them. No expiration on requests.

### Medical Record Structure
- **D-04:** Vaccination records: vaccine name, date administered, batch/lot number, next due date, administering vet, notes/reactions. Full record for tracking.
- **D-05:** Medication records: medication name, dosage, frequency (e.g., "twice daily"), start date, end date, prescribing vet, reason/diagnosis, instructions, side effects noted. Full record.
- **D-06:** Visit notes use SOAP format: Subjective (owner's description), Objective (findings), Assessment (diagnosis), Plan (treatment). Professional medical standard.
- **D-07:** All medical records linked to Pet via PetId FK. Records have CreatedAt/UpdatedAt timestamps. Vet records include VetUserId FK.

### Vet Area Layout
- **D-08:** Full Vet area (`Areas/Vet/`) with sidebar navigation, dashboard, and multiple controllers. Professional, scalable — mirrors Admin area structure.
- **D-09:** Vet dashboard shows: stat cards (total assigned pets, pending requests, recent records), assigned pets list with quick actions, pending assignment requests to accept/reject, availability calendar view.
- **D-10:** Vet can manage their own profile (clinic, specialty, bio, services, availability). Profile editable from vet area.

### Pet Owner Medical View
- **D-11:** Summary on Pet Details page — recent medical records (last 5) with type badges. Full history on separate Medical History page per pet.
- **D-12:** Medical history timeline: unified chronological view with type badges (Vaccination, Medication, Visit), plus filter tabs to show only one type. Most flexible approach.

### Admin Expansion
- **D-13:** Full vet admin panel — Admin can manage vet profiles, assign vets to pets, view all pet medical records, generate reports. Dedicated admin pages for vet management.
- **D-14:** Admin can view vet profiles, approve/reject vet registrations if needed, manage vet assignments, view medical records across all pets.

### Agent's Discretion
- Pagination, sorting, and table/list UX patterns — planner decides (reuses existing patterns)
- Calendar implementation details — planner decides (jQuery datepicker or simple table)
- Medical record forms layout — planner decides
- EF Core configurations and migrations — planner decides
- PDF/printable view for medical history — planner decides if in scope

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Requirements
- `.planning/REQUIREMENTS.md` — MED-01 through MED-04 define the medical records feature scope
- `.planning/ROADMAP.md` §Phase 5 — Goal, success criteria, dependencies on Phase 1 and Phase 3

### Architecture & Patterns
- `.planning/PROJECT.md` — Clean Architecture (4 layers), tech stack (ASP.NET Core MVC, EF Core, SQL Server, Razor Views + jQuery + Tailwind)
- `.planning/PROJECT.md` §Key Decisions — Claims-based Permissions, Admin dashboard foundation, Vet dashboard in Phase 5

### Existing Code Patterns
- `src/PetPlatform.Infrastructure/Persistence/ApplicationDbContext.cs` — DbContext pattern, entity registration
- `src/PetPlatform.Domain/Entities/Pet.cs` — Pet entity structure (reference for medical record FK)
- `src/PetPlatform.Application/Services/AdoptionService.cs` — Service pattern (interface + Result<T> + FluentValidation)
- `src/PetPlatform.Application/Interfaces/IAdoptionService.cs` — Interface pattern
- `src/PetPlatform.Host.MVC/Areas/Admin/Controllers/` — Admin area controller pattern
- `src/PetPlatform.Host.MVC/Areas/Admin/Views/Shared/_AdminLayout.cshtml` — Admin sidebar layout (template for Vet area)
- `src/PetPlatform.Infrastructure/Identity/SeedData.cs` — Role seeding (Vet role exists but unused)

### Prior Phase Context
- `.planning/phases/04-lost-pets/04-CONTEXT.md` — Phase 4 decisions (D-01 through D-16) — reuse service/controller patterns
- `.planning/phases/03-adoption-module/03-CONTEXT.md` — Phase 3 decisions — reuse adoption pattern for vet assignments

### No External Specs
No external specs — requirements fully captured in decisions above.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `Pet` entity (`Domain/Entities/Pet.cs`): Species enum, breed, age, weight — medical records link via PetId FK
- `IFileStorageService` (`Application/Interfaces/IFileStorageService.cs`): Photo upload for vet profile photos
- `Result<T>` pattern (`Application/Common/Result.cs`): Service error handling
- FluentValidation (`Application/Validators/`): Input validation for medical record forms
- `PetSpecies` enum (`Domain/Enums/PetSpecies.cs`): Species values for vet specialty filtering
- `ApplicationDbContext` (`Infrastructure/Persistence/ApplicationDbContext.cs`): Register new DbSets

### Established Patterns
- **Clean Architecture layers:** Domain (entities/enums) → Application (services/interfaces/DTOs) → Infrastructure (EF Core/Identity) → Host.MVC (controllers/views)
- **Service pattern:** Interface in Application, implementation with DbContext + validators, Result<T> for errors
- **Controller pattern:** Constructor DI, async actions, View-based responses
- **Area organization:** Admin/, Customer/, ServiceProvider/ areas with Controllers/ and Views/ subdirs
- **Admin layout:** `_AdminLayout.cshtml` with sidebar navigation — template for Vet area
- **EF Core:** Code-First migrations, configurations in Infrastructure/Persistence/Configurations/

### Integration Points
- `ApplicationDbContext` — register new DbSets for VetProfile, VetAssignment, VaccinationRecord, MedicationRecord, VetVisitNote
- Identity system — Vet role exists, needs VetProfile entity linked to ApplicationUser
- Admin area — expand with vet management pages, update `_AdminLayout.cshtml` sidebar
- Pet entity — medical records link via PetId FK (already has OwnerId for pet owner access)
- Customer area — add medical history view for pet owners
- New Vet area — create from scratch with sidebar, dashboard, controllers

</code_context>

<specifics>
## Specific Ideas

No specific requirements — open to standard approaches.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 5-Medical Records & Admin Expansion*
*Context gathered: 2026-07-21*
