# Phase 3: Adoption Module - Research

**Research date:** 2026-07-20
**Phase:** 3 - Adoption Module

## Codebase Analysis

### Existing Patterns Found

**Domain Entities:** Private setters, private parameterless constructor for EF Core, static `Create()` factory methods with `Guard.Against` validation, instance methods for mutations that set `UpdatedAt`. (`Pet.cs`, `Order.cs`)

**Status Workflow:** Enums as int-backed, forward-only transitions enforced in domain method (`Order.UpdateStatus`), `OrderStatusHistory` entity to log every transition. (`Order.cs:40-51`)

**Service Layer:** Constructor-injected `IApplicationDbContext` + `IValidator<T>`, FluentValidation before domain creation, `Result<T>` return type for success/failure, ownership checks via userId comparison. (`PetService.cs`, `OrderService.cs`)

**Controller Pattern:** Constructor-injected services, `User.FindFirst(ClaimTypes.NameIdentifier)` for userId extraction, `[ValidateAntiForgeryToken]` on POSTs, `ViewData`/`ViewBag` for filter state passthrough. (`MyAccountController.cs`, `CatalogController.cs`)

**Area Pattern:** `[Area("Customer")]` + `[Authorize]` attributes on area controllers. (`MyAccountController.cs:8-9`)

**Catalog/Search:** Filter DTO passed to service, nullable parameters for optional filters, queryable LINQ chaining, pagination via `page` param and `PageSize` constant. (`CatalogController.cs:23-44`)

**EF Core Configurations:** `IEntityTypeConfiguration<T>` classes in `Persistence/Configurations/`, auto-discovered via `ApplyConfigurationsFromAssembly`. Enums stored as `int` via `HasConversion<int>()`. Indexes on FK columns and frequently-filtered columns. (`OrderConfiguration.cs`, `PetConfiguration.cs`)

### Reusable Assets

| Asset | Reuse For |
|---|---|
| `Pet` entity & `PetConfiguration` | FK target for AdoptionListing |
| `Order` status workflow pattern | ApplicationStatus lifecycle (forward-only, history log) |
| `Result<T>` | Service return types for validation/business errors |
| `IApplicationDbContext` | Add `DbSet<AdoptionListing>`, `DbSet<AdoptionApplication>` |
| `CatalogController` filter pattern | Adoption listing search (species, location, age) |
| `MyAccountController` area pattern | Customer adoption dashboard |
| `OrderStatusHistory` entity pattern | `ApplicationStatusHistory` for audit trail |
| `PetSpecies` enum | Filter for adoption listings |

### Integration Points

- **Pet ↔ AdoptionListing:** FK `AdoptionListing.PetId` → `Pet.Id`. One pet can have one active listing at a time (constraint enforced in service layer).
- **ServiceProvider ↔ Shelter:** Assuming a `ServiceProvider` area exists or will exist; shelter identity tied to `ApplicationUser` with a `ServiceProvider` role. AdoptionListing links to the shelter's UserId.
- **Authorization:** Customer area for browsing/submitting applications; ServiceProvider/Admin area for managing listings and reviewing applications. Role-based `[Authorize(Roles = "...")]` on shelter endpoints.
- **E-commerce overlap:** Adoption module is independent of the product catalog — no shared entities needed.

## Technical Approach

### Domain Model

**New Entities:**

`AdoptionListing` — represents a pet available for adoption
- `Id` (int, PK)
- `PetId` (int, FK → Pet)
- `ShelterUserId` (string, FK → ApplicationUser, the shelter posting the listing)
- `Title` (string, required, max 200)
- `Description` (string?, nullable, max 2000)
- `Location` (string, required, max 200) — city/region for search
- `Status` (ListingStatus enum)
- `CreatedAt`, `UpdatedAt`

`AdoptionApplication` — represents a user's adoption request
- `Id` (int, PK)
- `ListingId` (int, FK → AdoptionListing)
- `ApplicantUserId` (string, FK → ApplicationUser)
- `Message` (string?, nullable, max 1000) — why the applicant wants this pet
- `Status` (ApplicationStatus enum)
- `ReviewedByUserId` (string?, nullable) — shelter user who acted
- `ReviewNotes` (string?, nullable, max 500)
- `CreatedAt`, `UpdatedAt`

`ApplicationStatusHistory` — mirrors `OrderStatusHistory` pattern
- `Id` (int, PK)
- `ApplicationId` (int, FK → AdoptionApplication)
- `Status` (ApplicationStatus)
- `ChangedAt` (DateTime)

**New Enums:**

```
ListingStatus { Active = 0, Pending = 1, Adopted = 2, Closed = 3 }
ApplicationStatus { Submitted = 0, UnderReview = 1, Approved = 2, Rejected = 3, Withdrawn = 4 }
```

### Service Layer

**IAdoptionService / AdoptionService:**

Customer-facing:
- `GetActiveListingsAsync(species?, location?, searchTerm?, page)` — paginated search
- `GetListingByIdAsync(int id)` — detail view
- `SubmitApplicationAsync(CreateApplicationDto, applicantUserId)` — creates application with `Submitted` status, validates listing is `Active`, prevents duplicate applications per user per listing
- `GetMyApplicationsAsync(string userId)` — applicant's applications
- `WithdrawApplicationAsync(int appId, string userId)` — set to `Withdrawn` if still `Submitted`

Shelter-facing:
- `GetShelterListingsAsync(string shelterUserId)` — shelter's own listings
- `CreateListingAsync(CreateListingDto, shelterUserId)` — creates with `Active` status
- `UpdateListingAsync(int id, UpdateListingDto, shelterUserId)` — edit listing
- `CloseListingAsync(int id, shelterUserId)` — set to `Closed`
- `GetApplicationsForListingAsync(int listingId, shelterUserId)` — list applications
- `ReviewApplicationAsync(int appId, ReviewApplicationDto, reviewerUserId)` — approve/reject with notes, updates listing status to `Adopted` on approval

All mutation methods return `Result<T>`. Validators via FluentValidation. Ownership checks in every method.

### Controller/UI Layer

**Public:**
- `AdoptionController` (root area) — `Index` (search/browse), `Details` (listing detail)

**Customer Area:**
- `Customer/AdoptionController` — `MyApplications` (list), `Apply` (GET/POST), `Withdraw` (POST)

**ServiceProvider Area (shelter management):**
- `ServiceProvider/AdoptionController` — `Listings` (manage), `CreateListing` (GET/POST), `EditListing` (GET/POST), `Applications` (per listing), `ReviewApplication` (GET/POST)

**Views:** Razor views following existing layout patterns. Listing search page uses catalog-style filter sidebar.

### Database

- Add `DbSet<AdoptionListing>`, `DbSet<AdoptionApplication>`, `DbSet<ApplicationStatusHistory>` to both `IApplicationDbContext` and `ApplicationDbContext`
- New configuration classes: `AdoptionListingConfiguration`, `AdoptionApplicationConfiguration`, `ApplicationStatusHistoryConfiguration`
- Follow existing patterns: `HasMaxLength`, `IsRequired`, `HasConversion<int>()` for enums, indexes on `PetId`, `ShelterUserId`, `ApplicantUserId`, `Status`
- Single migration for all three entities

## Risks and Considerations

- **One active listing per pet:** Must enforce at service level (check for existing `Active` listing on `PetId` before creating new one). No DB unique constraint on (PetId + Status) since EF Core filtered unique indexes require raw SQL.
- **Application status branching:** Unlike Order's linear flow, ApplicationStatus has a branch (Approved/Rejected from UnderReview). The domain `UpdateStatus` method needs a set of allowed transitions rather than simple "forward-only" arithmetic comparison.
- **Shelter identity mapping:** Need to confirm how ServiceProvider role is implemented. If no ServiceProvider area exists yet, this phase may need to create it or use Admin area as fallback.
- **Search performance:** Location search via `Contains` is acceptable for MVP. If performance becomes an issue, add a computed search column or use full-text search in a later phase.
- **Concurrent applications:** Multiple users may apply to the same listing simultaneously. Approval of one should not prevent others from being rejected — no hard conflict, but status checks prevent double-approval.

## Recommendations

1. **Mirror the Order pattern** for adoption applications — it's the closest existing analogue (entity with status workflow + history tracking). Copy `OrderStatusHistory` structure for `ApplicationStatusHistory`.
2. **Use the CatalogController filter pattern** for listing search — pass a filter DTO with nullable params, paginate results. This gives consistent UX with the existing product catalog.
3. **Create a dedicated `IAdoptionService`** rather than extending `IPetService` — adoption has distinct business rules (listings, applications, shelter-specific operations) that don't belong in pet CRUD.
4. **Use Area routing for role separation:** Customer area for applicants, ServiceProvider area for shelter operators. This matches the existing `Areas/Customer` pattern and keeps authorization clean.
5. **Status transition logic:** Implement a dictionary of allowed transitions in the domain entity rather than relying on enum ordering (which breaks with branching flows like Approved/Rejected).
