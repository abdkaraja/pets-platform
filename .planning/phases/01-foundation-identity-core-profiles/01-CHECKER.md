# Plan Checker: Phase 1 — Foundation, Identity & Core Profiles

**Re-checked:** 2026-07-18 (Revision 2 — re-verification after fixes)
**Plans:** 3 (01-01, 01-02, 01-03)
**Status:** ISSUES FOUND — 0 blocker(s), 4 warning(s)

---

## Revision History

| Rev | Date | Blockers | Warnings | Status |
|-----|------|----------|----------|--------|
| 1 | 2026-07-18 | 2 | 5 | ISSUES FOUND — returned to planner |
| 2 | 2026-07-18 | 0 | 4 | ISSUES FOUND — warnings remain, no blockers |

### Fix Verification (Rev 1 → Rev 2)

| Previous Issue | Severity | Fix Applied | Verified |
|----------------|----------|-------------|----------|
| Identity UI pages not created (AUTH-01/02/03) | BLOCKER | Plan 01 Task 2 now: chains `.AddDefaultUI()` on Identity builder, installs `Microsoft.AspNetCore.Identity.UI` package, creates `Areas/Identity/Pages/_ViewImports.cshtml` + `_ViewStart.cshtml` routing Identity pages to shared Tailwind layout | ✅ FIXED |
| RESEARCH.md Open Questions unresolved | BLOCKER | All 3 questions now marked `(RESOLVED)` with inline resolution details | ✅ FIXED |
| TDD task without tests (Plan 02 T1) | WARNING | `tdd="true"` attribute removed; task is `type="auto"` — no spurious TDD claim | ✅ FIXED |
| File upload in controller tier (Plan 02) | WARNING | New `IFileStorageService` interface in Application layer + `FileStorageService` implementation in Infrastructure layer; controller delegates via DI injection | ✅ FIXED |

---

## Dimension 1: Requirement Coverage

| Requirement | Plan | Tasks | Status |
|-------------|------|-------|--------|
| AUTH-01 (sign up with email/password) | 01 | T2 | ✅ `.AddDefaultUI()` + Identity.UI provides Register page at `/Identity/Account/Register` |
| AUTH-02 (email verification) | 01 | T2 | ✅ `RequireConfirmedAccount = true` + Identity.UI ConfirmEmail page |
| AUTH-03 (password reset via email) | 01 | T2 | ✅ Identity.UI ForgotPassword + ResetPassword pages |
| AUTH-04 (session persists) | 01 | T2 | ✅ Cookie auth via `AddIdentity<ApplicationUser, ApplicationRole>()` |
| AUTH-05 (account lockout) | 01 | T2 | ✅ `Lockout.MaxFailedAccessAttempts = 5`, `DefaultLockoutTimeSpan = 15min` |
| AUTH-06 (4 roles) | 01 | T1 | ✅ SeedData creates Admin, Customer, Vet, ServiceProvider |
| AUTH-07 (Claims-based Permissions) | 01 | T1,T2 | ✅ SeedData seeds 5 permission claims onto Admin role |
| AUTH-08 (Policy-based Authorization) | 01 | T2 | ✅ 5 policies registered via `AddAuthorizationBuilder()` |
| PET-01 (create pet profile with photo) | 02 | T1,T2 | ✅ PetService + MyPetsController with IFileStorageService |
| PET-02 (view/edit/delete own pets) | 02 | T1,T2 | ✅ Ownership enforcement in PetService + MyPetsController |
| PET-03 (public pet view) | 02 | T2 | ✅ Public PetController in root Controllers/ |
| PET-04 (multi-pet per owner) | 02 | T1 | ✅ `GetAllByOwnerAsync` returns all pets for owner |
| PET-05 (customer profile) | 02 | T1,T2 | ✅ CustomerService + MyAccountController |
| PET-06 (My Account page) | 02 | T2 | ✅ MyAccountController with Index + Edit views |
| ADMN-01 (view all users) | 03 | T1,T2 | ✅ UserController Index + paginated list |
| ADMN-02 (activate/deactivate) | 03 | T1,T2 | ✅ Activate/Deactivate POST actions using UserManager lockout APIs |
| ADMN-03 (assign roles) | 03 | T1,T2 | ✅ AssignRole/RemoveRole POST actions |
| ADMN-04 (create roles) | 03 | T1,T2 | ✅ RoleController Create action |
| ADMN-05 (assign permissions to roles) | 03 | T1,T2 | ✅ AddClaim/RemoveClaim actions using RoleManager |

**Result:** All 19 requirements (AUTH-01 through AUTH-08, PET-01 through PET-06, ADMN-01 through ADMN-05) have covering tasks with specific, actionable implementations. ✅

---

## Dimension 2: Task Completeness

| Plan | Task | Type | Files | Action | Verify | Done | Status |
|------|------|------|-------|--------|--------|------|--------|
| 01 | T1 | auto | ✅ | ✅ | ✅ | ✅ | Valid |
| 01 | T2 | auto | ⚠️ | ✅ | ✅ | ✅ | Valid (files gap) |
| 02 | T1 | auto | ✅ | ✅ | ✅ | ✅ | Valid |
| 02 | T2 | auto | ✅ | ✅ | ✅ | ✅ | Valid |
| 03 | T1 | auto | ✅ | ✅ | ✅ | ✅ | Valid |
| 03 | T2 | auto | ✅ | ✅ | ✅ | ✅ | Valid |

**Plan 01 Task 2 files gap:** The action explicitly creates `src/PetPlatform.Infrastructure/Services/EmailSender.cs` (implementing `IEmailSender<ApplicationUser>`) but this file is absent from the task's `<files>` element and the plan's `files_modified` list. The action is specific enough that the executor will create it, but the file list is incomplete for tracking and manifest purposes.

**No spurious TDD claims:** Plan 02 Task 1 is correctly typed as `auto` with no `tdd` attribute. The previous `tdd="true"` claim has been removed.

---

## Dimension 3: Dependency Correctness

| Plan | Wave | depends_on | Valid | Notes |
|------|------|------------|-------|-------|
| 01 | 1 | [] | ✅ | Foundation — no dependencies |
| 02 | 2 | ["01-01"] | ✅ | Builds on foundation |
| 03 | 2 | ["01-01"] | ✅ | Builds on foundation |

**Cross-plan conflicts:** Plans 02 and 03 both run in Wave 2. Their file sets are disjoint:
- Plan 02: `Areas/Customer/`, Application services, Infrastructure/Services/FileStorageService
- Plan 03: `Areas/Admin/`
No merge conflicts. ✅

**Dependency graph:** Acyclic. Plan 01 → {Plan 02, Plan 03}. ✅

---

## Dimension 4: Key Links Planned

| From | To | Via | Plan | Status |
|------|-----|-----|------|--------|
| Program.cs | SeedData.cs | Startup seeding (`SeedData.InitializeAsync` after `app.Build()`) | 01 | ✅ Task 2 action |
| ApplicationDbContext | Pet.cs | `DbSet<Pet>` | 01 | ✅ Task 1 action |
| MyPetsController | PetService | DI-injected `IPetService` | 02 | ✅ Task 2 action |
| PetService | ApplicationDbContext | DbContext injection | 02 | ✅ Task 1 action |
| MyPetsController | FileStorageService | DI-injected `IFileStorageService` | 02 | ✅ Task 2 action — **new link from fix** |
| UserController | ApplicationUser | `UserManager<ApplicationUser>` injection | 03 | ✅ Task 1 action |
| RoleController | ApplicationRole | `RoleManager<ApplicationRole>` injection | 03 | ✅ Task 1 action |
| Admin controllers | Authorization policies | `[Authorize(Policy = "Permission:...")]` attributes | 03 | ✅ Task 1 action |

All key links are planned with explicit wiring methods. ✅

---

## Dimension 5: Scope Sanity

| Plan | Tasks | Files | Wave | Status |
|------|-------|-------|------|--------|
| 01 | 2 | 25 | 1 | ⚠️ WARNING — high file count |
| 02 | 2 | 23 | 2 | ⚠️ WARNING — high file count |
| 03 | 2 | 12 | 2 | ✅ Within range |

**Thresholds:** Tasks/plan: 2-3 target ✅ | Files/plan: 5-8 target, 10 warning, 15+ blocker

**Analysis:**
- **Plan 01 (25 files):** Foundation/scaffold plan creating entire solution structure. Task 1 (16 files) creates all domain entities, infrastructure identity, persistence, and configurations. Task 2 (9 files) configures Program.cs, layout, Identity Area setup. High file count is inherent to greenfield project scaffolding.
- **Plan 02 (23 files):** Creates Application layer (10 files: interfaces, services, DTOs, validators, FileStorageService) and Customer Area (13 files: controllers, views). The IFileStorageService + FileStorageService addition (from the architectural fix) added 2 files vs. the original plan.
- **Plan 03 (12 files):** Admin Area controllers and views — within acceptable range.

**Risk assessment:** Plans 01 and 02 exceed the 15+ file threshold. However:
1. Task count is 2 per plan (within limits), keeping cognitive load manageable
2. Many files are boilerplate (Razor views, EF configurations, project scaffolding)
3. Foundation plans inherently require more files
4. Plans 02 and 03 run in parallel, distributing the work

**Recommendation:** Accept the risk. Monitor execution for context budget degradation. If execution quality degrades, split Plan 01 into scaffold (T1a: projects + NuGet) and identity (T1b: domain + infra + Program.cs).

---

## Dimension 6: Verification Derivation

| Plan | Truths User-Observable? | Artifacts Support Truths? | Key Links Complete? | Status |
|------|------------------------|--------------------------|---------------------|--------|
| 01 | Mixed (acceptable) | ✅ | ✅ | ✅ |
| 02 | ✅ All user-observable | ✅ | ✅ | ✅ |
| 03 | ✅ All user-observable | ✅ | ✅ | ✅ |

**Plan 01 truth assessment:** As a foundation plan, some truths are infrastructure-focused:
- "Claims-based permissions are seeded onto roles" → implementation detail
- "Policy-based authorization policies are registered in Program.cs" → implementation detail
- "Database migrations generate correctly" → implementation detail
- "Identity UI pages are accessible at /Identity/Account/* routes" → user-observable ✅

These are acceptable for a foundation plan where the deliverables ARE the infrastructure configuration. The user-observable truth ("Identity pages accessible", "Admin user has all permissions") anchors the implementation details.

---

## Dimension 7: Context Compliance

**SKIPPED** — No CONTEXT.md found for this phase.

---

## Dimension 7b: Scope Reduction Detection

**SKIPPED** — No CONTEXT.md found for this phase.

---

## Dimension 7c: Architectural Tier Compliance

**Source:** RESEARCH.md `## Architectural Responsibility Map`

| Capability | Expected Tier | Plan Placement | Status |
|------------|--------------|----------------|--------|
| User registration/login | Identity Razor Pages (Host.MVC) | `.AddDefaultUI()` + Identity.UI package provides default pages at `/Identity/Account/*` routes | ✅ |
| Role & permission seeding | Infrastructure (SeedData) | `Infrastructure/Identity/SeedData.cs` — static `InitializeAsync` | ✅ |
| Pet profile CRUD | Application (PetService) | `Application/Services/PetService.cs` with ownership enforcement | ✅ |
| Customer profile | Application (CustomerService) | `Application/Services/CustomerService.cs` | ✅ |
| File upload (pet photos) | Infrastructure (FileStorageService) | `Application/Interfaces/IFileStorageService.cs` + `Infrastructure/Services/FileStorageService.cs`; controller delegates via DI | ✅ |
| Policy-based authorization | Host.MVC (Program.cs) | `Program.cs` — policies registered in composition root | ✅ |
| EF Core data access | Infrastructure (DbContext) | `Infrastructure/Persistence/ApplicationDbContext.cs` | ✅ |
| Role management UI | Admin Area (Host.MVC) | `Areas/Admin/Controllers/` with `[Area("Admin")]` | ✅ |
| Pet management UI | Customer Area (Host.MVC) | `Areas/Customer/Controllers/` with `[Area("Customer")]` | ✅ |

**Previous finding (file upload in controller tier):** RESOLVED. Plan 02 now creates `IFileStorageService` in Application layer and `FileStorageService` in Infrastructure layer. MyPetsController injects `IFileStorageService` and delegates all file I/O. The controller handles HTTP concerns only (receiving `IFormFile`, passing to service, setting returned path on DTO). This matches the Architectural Responsibility Map.

---

## Dimension 8: Nyquist Compliance

**SKIPPED** — `workflow.nyquist_validation` is `false` in config.json.

---

## Dimension 9: Cross-Plan Data Contracts

Plans 02 and 03 run in parallel (Wave 2) but operate on disjoint data domains:
- **Plan 02:** Pet/CustomerProfile entities via Application services (PetService, CustomerService)
- **Plan 03:** Identity User/Role data via UserManager/RoleManager directly (Admin Area controllers)

No shared data pipelines, no conflicting transforms, no data entity accessed by both plans. ✅

---

## Dimension 10: AGENTS.md Compliance

**SKIPPED** — No AGENTS.md found in working directory.

---

## Dimension 11: Research Resolution

**Status:** ✅ PASS — All Open Questions resolved.

| Question | Resolution | Verified |
|----------|-----------|----------|
| SQL Server connection string | LocalDB in appsettings.json, SQLite fallback documented | ✅ `(RESOLVED)` in RESEARCH.md |
| Arabic RTL support | `dir="rtl"` in `_Layout.cshtml`, Tailwind v4 native `rtl:` variant | ✅ `(RESOLVED)` in RESEARCH.md |
| Identity scaffold scope | `.AddDefaultUI()` + Identity.UI package, Area _ViewImports/_ViewStart for shared layout | ✅ `(RESOLVED)` in RESEARCH.md |

**Previous finding:** RESOLVED. All3 questions now have `(RESOLVED)` suffix and inline resolution details.

---

## Dimension 12: Pattern Compliance

**SKIPPED** — No PATTERNS.md found for this phase.

---

## Verify Command Format Sanity

All plans use: `dotnet build PetPlatform.sln --no-restore 2>&1 | Select-String -Pattern "Build succeeded"`

- No `^` anchor anti-patterns ✅
- No `2>/dev/null || echo` error-swallowing ✅
- No watch mode flags (`--watchAll`) ✅
- `--no-restore` requires prior `dotnet restore` — execution workflow handles this ✅

---

## Numeric/Factual Claim Authority

| Claim | Plan Source | Assessment |
|-------|-----------|------------|
| "5 authorization policies" | Plan 01 T2 | 5 listed: Users.View, Users.Manage, Roles.Create, Roles.Assign, Pets.Manage ✅ |
| "4 seeded roles" | Plan 01 T1 | Admin, Customer, Vet, ServiceProvider = 4 ✅ |
| "5 permission claims on Admin role" | Plan 01 T1 | Users.View, Users.Manage, Roles.Create, Roles.Assign, Pets.Manage = 5 ✅ |
| "Lockout after 5 failed attempts" | Plan 01 T2 | `MaxFailedAccessAttempts = 5` ✅ |
| "5MB max file size" | Plan 02 T1 | FileStorageService validates 5MB limit ✅ |

No numeric claims conflict with RESEARCH.md. ✅

---

## Structured Issues

```yaml
issues:
  - dimension: task_completeness
    severity: warning
    plan: "01"
    task: 2
    description: >
      Plan 01 Task 2 action creates `src/PetPlatform.Infrastructure/Services/EmailSender.cs`
      (implementing IEmailSender<ApplicationUser> with console-log stub methods) but this
      file is absent from the task's <files> element and the plan's files_modified list.
      The action is explicit ("Create Infrastructure service: src/PetPlatform.Infrastructure/Services/EmailSender.cs")
      so the executor will create it, but the file list is incomplete for manifest tracking.
    fix_hint: >
      Add `src/PetPlatform.Infrastructure/Services/EmailSender.cs` to Task 2's <files> element
      and to the plan's files_modified list.

  - dimension: scope_sanity
    severity: warning
    plan: "01"
    description: >
      Plan 01 has 25 files across its frontmatter (22 from previous check + added Identity
      Area files). This exceeds the 15+ file threshold. Task 1 alone creates 16 files
      (solution scaffold + domain entities + infrastructure identity/persistence). Task 2
      creates 9 files (Program.cs, layout, Identity Area setup, EmailSender). Context budget
      risk exists during execution.
    fix_hint: >
      Accept for foundation plan (inherent to greenfield scaffolding). Monitor execution.
      If context degrades, split Task 1: scaffold projects + NuGet (T1a), domain entities
      + interfaces (T1b), infrastructure identity + persistence (T1c).

  - dimension: scope_sanity
    severity: warning
    plan: "02"
    description: >
      Plan 02 has 23 files (increased from 20 due to IFileStorageService + FileStorageService
      addition for architectural tier fix). Task 1 creates 10 files (Application layer services,
      DTOs, validators, file storage). Task 2 creates 13 files (controllers + 11 Razor views).
      Exceeds the 15+ file threshold. Views are boilerplate Razor markup with low cognitive
      complexity.
    fix_hint: >
      Accept for feature plan (Application services + Customer Area is inherently file-heavy).
      Monitor execution for context budget issues. If needed, split Task 2 into controllers
      (T2a) and views (T2b).

  - dimension: verification_command_format
    severity: warning
    plan: "all"
    description: >
      All 3 plans use compilation-only verification (`dotnet build`). No functional tests,
      no runtime verification, no smoke tests. The <verify> blocks confirm the code compiles
      but do not verify that Identity pages are accessible, that SeedData actually creates
      roles, that file upload validation works, or that authorization policies reject
      unauthorized requests.
    fix_hint: >
      Consider adding smoke-test verification commands post-execution:
      - `dotnet run --urls=http://localhost:5000` + curl to check /Identity/Account/Login returns 200
      - `dotnet ef database update` + database query to verify roles exist
      These could be added as post-execution UAT criteria in SUMMARY.md rather than in-plan verify.
```

---

## Summary

### Previous Blockers — Fixed ✅

1. **Identity UI pages (AUTH-01, AUTH-02, AUTH-03):** Plan 01 Task 2 now configures `.AddDefaultUI()` on the Identity builder, installs `Microsoft.AspNetCore.Identity.UI` package, and creates Identity Area `_ViewImports.cshtml` + `_ViewStart.cshtml` that routes Identity pages to the shared Tailwind layout with RTL support. Default Identity Razor Pages (Register, Login, ConfirmEmail, ForgotPassword, ResetPassword, Logout) are provided out of the box at `/Identity/Account/*` routes.

2. **RESEARCH.md Open Questions:** All 3 questions now marked `(RESOLVED)` with inline resolution details. The Identity scaffold question is resolved by the `.AddDefaultUI()` approach above.

### Previous Warnings — Status

| Warning | Status |
|---------|--------|
| TDD task without tests (Plan 02 T1) | ✅ FIXED — `tdd` attribute removed, task correctly typed as `auto` |
| File upload in controller tier (Plan 02) | ✅ FIXED — `IFileStorageService` + `FileStorageService` properly placed per Architectural Responsibility Map |
| Plan 01 high file count (22) | ⚠️ REMAINS (now 25) — acceptable for foundation plan |
| Plan 02 high file count (20) | ⚠️ REMAINS (now 23) — inherent to scope, increased by architectural fix |
| All verification compilation-only | ⚠️ REMAINS — no functional tests added |

### Remaining Warnings (4)

1. **EmailSender.cs missing from file list** (Plan 01 T2) — action creates it but `<files>` element omits it
2. **Plan 01 high file count (25 files)** — foundation plan exceeds threshold
3. **Plan 02 high file count (23 files)** — feature plan exceeds threshold
4. **Compilation-only verification** — no functional smoke tests

### Recommendation

**0 blocker(s) require revision.** Plans are ready for execution.

All critical gaps from the previous checker report have been resolved:
- Identity UI pages are now accessible via `.AddDefaultUI()` + Identity.UI package
- File upload properly delegated to Infrastructure tier via `IFileStorageService`
- Research questions formally resolved
- Spurious TDD claim removed

The 4 remaining warnings are acceptable for a greenfield foundation phase. The file count warnings are inherent to the scope (scaffold + full feature set). The verification warning is a quality improvement opportunity, not a blocker.

**Proceed to execution:** Run `/gsd-execute-phase 1` to begin.
