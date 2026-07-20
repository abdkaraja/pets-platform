# Phase 4: Lost Pets Module - Context

**Gathered:** 2026-07-20
**Status:** Ready for planning

<domain>
## Phase Boundary

Users can report lost or found pets with details, search lost pet reports by species/breed/color/location/date, and receive basic in-app alerts when new reports match their area — creating community-driven pet recovery.

</domain>

<decisions>
## Implementation Decisions

### Data Model
- **D-01:** Lost pet reports are a **separate `LostPetReport` entity** with optional `PetId` FK — reports can exist without a linked Pet (found pets with unknown owner). This keeps concerns separated like AdoptionListing does for Phase 3.
- **D-02:** Report type via `LostPetReportType` enum: `Lost`, `Found`. Single entity with type discriminator, not separate entities.
- **D-03:** Required fields: species, color, location (free-text city/area), date, description. Optional: breed, photos (1-5), linked PetId.
- **D-04:** Multiple photo uploads allowed (1-5) — reuses existing `IFileStorageService`. Photos stored as separate records referencing the report.

### Matching & Alerts
- **D-05:** In-app notifications only — no email infrastructure. Matches shown in a dashboard/notification center.
- **D-06:** Match = same species + same city/area (free-text contains match). Broad enough to catch potential matches, simple to implement.
- **D-07:** Alerts visible only to the original reporter — private, simple.
- **D-08:** Matching triggered on report creation — synchronous check for existing opposite-type reports. No background jobs.

### Location Handling
- **D-09:** Free-text city/area field — low barrier to entry, easy LIKE queries for search.
- **D-10:** Dedicated search page with filters (species, breed, color, location, date range) — reuses adoption listing pattern.
- **D-11:** Location search uses contains/partial match — more forgiving, catches variations.

### Report Lifecycle
- **D-12:** Statuses: `Open`, `Resolved`. Simple, covers main use case.
- **D-13:** Only the reporter can resolve their own report — prevents unauthorized changes.
- **D-14:** No automatic expiry — reports stay open until manually resolved.
- **D-15:** Reporter can edit while report is Open — allows updating details.
- **D-16:** All users can browse/search all open reports — public visibility for maximum community reach.

### Agent's Discretion
- Pagination, sorting, and search UX approach — planner decides (reuses adoption pattern)
- Notification center implementation details — planner decides
- Photo upload UI for multiple images — planner decides
- EF Core configurations and migrations — planner decides

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Requirements
- `.planning/REQUIREMENTS.md` — LOST-01 through LOST-03 define the lost pets feature scope
- `.planning/ROADMAP.md` §Phase 4 — Goal, success criteria, and dependency on Phase 1

### Architecture & Patterns
- `.planning/PROJECT.md` — Clean Architecture (4 layers), tech stack (ASP.NET Core MVC, EF Core, SQL Server, Razor Views + jQuery + Tailwind)
- `src/PetPlatform.Infrastructure/Persistence/ApplicationDbContext.cs` — DbContext pattern, entity registration
- `src/PetPlatform.Domain/Entities/Pet.cs` — Existing Pet entity structure (reference for LostPetReport relationship)

### Existing Code Patterns
- `src/PetPlatform.Application/Services/PetService.cs` — Service pattern (interface + Result<T> + FluentValidation)
- `src/PetPlatform.Application/Interfaces/IPetService.cs` — Interface pattern
- `src/PetPlatform.Host.MVC/Controllers/PetController.cs` — Controller pattern
- `src/PetPlatform.Host.MVC/Areas/Customer/Controllers/` — Area-based controller organization
- `.planning/phases/03-adoption-module/03-CONTEXT.md` — Prior phase context decisions (D-01 through D-07) — reuse patterns

### No External Specs
No external specs — requirements fully captured in decisions above.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `Pet` entity (Domain/Entities/Pet.cs): Species enum, breed, age, weight, photo — LostPetReport references via optional FK
- `IFileStorageService` (Application/Interfaces/IFileStorageService.cs): Photo upload handling for report images (1-5 photos)
- `Result<T>` pattern (Application/Common): Service error handling
- FluentValidation (Application/Validators): Input validation for report forms
- `PetSpecies` enum (Domain/Enums/PetSpecies.cs): Species values for search filters

### Established Patterns
- **Clean Architecture layers:** Domain (entities/enums) → Application (services/interfaces/DTOs) → Infrastructure (EF Core/Identity) → Host.MVC (controllers/views)
- **Service pattern:** Interface in Application, implementation with DbContext + validators, Result<T> for errors
- **Controller pattern:** Constructor DI, async actions, View-based responses
- **Area organization:** Admin/, Customer/, Identity/ areas with Controllers/ and Views/ subdirs
- **EF Core:** Code-First migrations, configurations in Infrastructure/Persistence/Configurations/

### Integration Points
- `ApplicationDbContext` — register new DbSets for LostPetReport, LostPetReportPhoto, MatchNotification
- Identity system — any user can report lost/found pets (no special role required)
- Customer area — reporter-facing views (report lost/found, search reports, view matches, manage own reports)
- Pet entity — optional FK from LostPetReport.PetId → Pet.Id

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

*Phase: 4-Lost Pets Module*
*Context gathered: 2026-07-20*
