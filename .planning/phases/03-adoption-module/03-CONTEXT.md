# Phase 3: Adoption Module - Context

**Gathered:** 2026-07-20
**Status:** Ready for planning

<domain>
## Phase Boundary

Users can discover adoptable pets through search and filtering, submit adoption applications with household information, and shelters (ServiceProvider role) can review, approve, or reject applications through a unified dashboard. Adopters receive status updates at each stage.

</domain>

<decisions>
## Implementation Decisions

### Data Model
- **D-01:** Adoptable listings are a **separate `AdoptionListing` entity** that references a `Pet` via foreign key. The existing `Pet` entity remains user-owned and unchanged. This keeps concerns separated — Pet is a profile, AdoptionListing is a discoverable listing.
- **D-02:** `AdoptionListing` captures **location, description, and listing status** (Active/Adopted/Closed) beyond the Pet reference. Most pet details (name, species, breed, age, weight, photo) come from the linked Pet. Minimal listing-specific data for v1.
- **D-03:** Listing status enum: `Active`, `Adopted`, `Closed`. Application status enum: `Submitted`, `Reviewed`, `Approved`, `Rejected`.

### Shelter Identity
- **D-04:** Shelter = `ServiceProvider` role. Their existing `CustomerProfile` doubles as shelter profile (name, address, contact). No new role or entity needed for shelter identity.
- **D-05:** Only ServiceProvider users can create and manage AdoptionListings. Authorization enforced via Claims-based Permissions (existing pattern).

### Application Workflow
- **D-06:** Application form captures household information and pet experience (per ADPT-03). Exact fields deferred to planner — the user chose not to discuss detailed form fields.
- **D-07:** Status updates at each stage of the application process (per ADPT-05). Exact notification mechanism deferred to planner — the user chose not to discuss notification details.

### the agent's Discretion
- Application form field details (household info, pet experience questions) — planner decides based on requirements
- Search/filter UX approach — planner decides whether to reuse catalog pattern or design differently
- Notification mechanism for status updates — planner decides implementation approach
- Pagination, sorting, and location handling for listings — planner decides

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Requirements
- `.planning/REQUIREMENTS.md` — ADPT-01 through ADPT-05 define the adoption feature scope
- `.planning/ROADMAP.md` §Phase 3 — Goal, success criteria, and dependency on Phase 1

### Architecture & Patterns
- `.planning/PROJECT.md` — Clean Architecture (4 layers), tech stack (ASP.NET Core MVC, EF Core, SQL Server, Razor Views + jQuery + Tailwind)
- `src/PetPlatform.Infrastructure/Persistence/ApplicationDbContext.cs` — DbContext pattern, entity registration
- `src/PetPlatform.Domain/Entities/Pet.cs` — Existing Pet entity structure (reference for AdoptionListing relationship)

### Existing Code Patterns
- `src/PetPlatform.Application/Services/PetService.cs` — Service pattern (interface + Result<T> + FluentValidation)
- `src/PetPlatform.Application/Interfaces/IPetService.cs` — Interface pattern
- `src/PetPlatform.Host.MVC/Controllers/PetController.cs` — Controller pattern
- `src/PetPlatform.Host.MVC/ Areas/Customer/Controllers/` — Area-based controller organization
- `src/PetPlatform.Host.MVC/Areas/Admin/Controllers/` — Admin dashboard pattern

### No External Specs
No external specs — requirements fully captured in decisions above.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `Pet` entity (Domain/Entities/Pet.cs): Species enum, breed, age, weight, photo — AdoptionListing references this
- `IFileStorageService` (Application/Interfaces/IFileStorageService.cs): Photo upload handling for listing images
- `Result<T>` pattern (Application/Common): Service error handling
- FluentValidation (Application/Validators): Input validation for application forms
- `PetSpecies` enum (Domain/Enums/PetSpecies.cs): Species values for search filters

### Established Patterns
- **Clean Architecture layers:** Domain (entities/enums) → Application (services/interfaces/DTOs) → Infrastructure (EF Core/Identity) → Host.MVC (controllers/views)
- **Service pattern:** Interface in Application, implementation with DbContext + validators, Result<T> for errors
- **Controller pattern:** Constructor DI, async actions, View-based responses
- **Area organization:** Admin/, Customer/, Identity/ areas with Controllers/ and Views/ subdirs
- **EF Core:** Code-First migrations, configurations in Infrastructure/Persistence/Configurations/

### Integration Points
- `ApplicationDbContext` — register new DbSets for AdoptionListing, AdoptionApplication
- Identity system — Claims-based Permissions for shelter vs customer authorization
- Customer area — adopter-facing views (browse listings, submit application, view status)
- Admin area — shelter dashboard for managing listings and reviewing applications
- Pet entity — foreign key from AdoptionListing.PetId → Pet.Id

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

*Phase: 3-Adoption Module*
*Context gathered: 2026-07-20*
