# Project Research Summary

**Project:** Pet Platform
**Domain:** Pet care platform web application (e-commerce, adoption, lost pets, medical records)
**Researched:** 2026-07-18
**Confidence:** HIGH

## Executive Summary

The Pet Platform is a comprehensive ASP.NET Core MVC web application built with Clean Architecture, serving pet owners, veterinarians, and service providers across six feature domains: authentication, e-commerce, adoption, lost pets, medical records, and admin management. Research across comparable platforms (Pawlly, VetCare, Petezy, PetClues, KeystoneCommerce) and failed pet startups (Vetster, Fuzzy, BarkBox, PetBacker) reveals a clear pattern: successful platforms build one vertical deeply before expanding, while failures attempt everything simultaneously. The recommended approach is .NET 10 with EF Core 10, SQL Server, ASP.NET Core Identity, Tailwind CSS v4, and jQuery — all server-rendered Razor Views per project constraints. Clean Architecture provides the structural foundation but must be implemented pragmatically: skip MediatR, AutoMapper, and generic repositories until real complexity demands them.

The primary risk is scope overexpansion. The project lists six major feature areas, but pet platform post-mortems consistently show that launching multiple modules simultaneously without proven unit economics leads to failure. Research strongly recommends validating one vertical (e-commerce) before expanding to adoption, lost pets, and medical records. A secondary critical risk is multi-tenant data isolation — if shelters and vet clinics share the platform, missing tenant filters create catastrophic data leaks that are invisible in single-tenant staging environments. A third risk is stale pet listings, which destroy user trust: 42.5% of shelter websites have stale listings, and adoption inquiries drop 95% when data isn't fresh.

The architecture research confirms a standard 4-layer Clean Architecture (Domain → Application → Infrastructure → Host.MVC) with ASP.NET Areas for role-based UI separation (Admin, Customer, Identity). Key patterns include Repository + Unit of Work for transactional consistency, Result\<T\> for type-safe error handling, and Claims-based permissions layered on ASP.NET Core Identity. The build order should follow dependency chains: authentication first (everything depends on it), pet profiles second (central entity), then independent modules (e-commerce, adoption, lost pets) in sequence, with medical records and vet dashboard last (they depend on established pet data).

## Key Findings

### Recommended Stack

The stack is well-established with high-confidence sources. All major technologies have LTS support aligned to .NET 10 (supported through Nov 2028). Tailwind CSS v4 has breaking changes from v3 — no `tailwind.config.js`, uses `@tailwindcss/cli` and `@source` directives — this must be set up correctly in Phase 0 or it will cause friction in every subsequent phase. jQuery 3.x is viable for AJAX and DOM manipulation; the deprecated `jquery-ajax-unobtrusive` package must not be used. Stripe.net 48.3.0 provides payment processing, but Cash on Delivery should be the initial implementation to validate the purchase flow before adding Stripe complexity.

**Core technologies:**
- **.NET 10 SDK (10.0.301+):** Runtime and C# 14 — latest LTS through Nov 2028, .NET 8 is nearing EOL
- **ASP.NET Core MVC 10.0:** Server-rendered Razor Views with tag helpers — project constraint, no SPA frameworks
- **Entity Framework Core 10.0.10:** ORM with Code-First migrations — must match .NET 10 exactly; named query filters and JSON column support
- **SQL Server 2022+:** Database — project requirement, EF Core 10 supports both 2022 and 2025
- **ASP.NET Core Identity 10.0:** Authentication with 4 roles (Admin, Customer, Vet, ServiceProvider) and Claims-based permissions
- **Tailwind CSS v4.x:** Utility-first CSS via `@tailwindcss/cli` — native RTL support for Arabic-first requirement
- **jQuery 3.7.x:** AJAX and DOM manipulation — direct `$.ajax()` calls, not deprecated unobtrusive library
- **Stripe.net 48.3.0:** Payment processing — defer until e-commerce checkout phase
- **FluentValidation 12.x:** Request/command validation in Application layer — replaces DataAnnotations
- **Serilog 4.x:** Structured logging with request context enrichment

**Avoid:** IdentityServer4 (overkill for cookie auth), Repository+UoW over EF Core (EF is already UoW), DataAnnotations for validation (use FluentValidation), Swashbuckle (built-in OpenAPI in .NET 9+), `tailwindcss` standalone CLI (v4 uses `@tailwindcss/cli`).

### Expected Features

Research identified 13 table-stakes features, 9 differentiators, and 8 anti-features to explicitly avoid. The MVP should launch with authentication, pet profiles, e-commerce (catalog + cart + checkout + orders), adoption (listings + search + applications), lost pets (report + search), medical records (basic), and admin dashboard foundation. Differentiators like vet SOAP dashboard, vaccination reminders, and emergency pet passport should be validated before building. Anti-features like real-time chat, GPS tracking, native mobile apps, and video consultations should be deferred to v2+ or avoided entirely.

**Must have (table stakes):**
- Authentication & role-based access (Admin, Customer, Vet, ServiceProvider) — root dependency, everything depends on this
- Pet profiles (CRUD, multi-pet per owner, basic health data) — central entity, second dependency
- Customer account management (My Account, order history, pet list) — user retention
- Product catalog with search/filter + shopping cart + checkout + order management — revenue model
- Adoption pet listings with search + application flow + shelter review — core value proposition
- Lost pet listing & search — community value, high user trust impact
- Medical records (vaccination history, medication records, visit notes) — vet integration foundation
- Admin dashboard (user management, role/permission management) — operational control
- Responsive design (mobile-first) — 60%+ of pet app usage is mobile

**Should have (competitive):**
- Vet dashboard with SOAP notes — when vet users confirm the workflow
- Vaccination/medication reminders — strong retention driver, users open app regularly
- Emergency pet passport — high user value, relatively simple (lightweight view of medical data)
- Digital adoption contracts with e-signatures — when adoption volume justifies it

**Defer (v2+):**
- AI-powered health insights — requires ML infrastructure and data volume
- Service provider marketplace — two-sided marketplace complexity
- Pet owner community/social features — high complexity, uncertain retention
- Native mobile apps — web-first with PWA capabilities first
- Video vet consultations — infrastructure + medical licensing
- Subscription boxes — inventory + logistics complexity

### Architecture Approach

A structured monolith with 4 Clean Architecture layers (Domain → Application → Infrastructure → Host.MVC) is the right default. This is validated by Microsoft's eShopOnWeb reference, VetCare (multi-tenant vet SaaS), and KeystoneCommerce (MVC e-commerce). The Domain layer defines entities with private setters and business methods (not an anemic data bag), repository interfaces, and value objects. The Application layer contains business logic services, DTOs, and FluentValidation validators using the Result\<T\> pattern. The Infrastructure layer houses EF Core DbContext, repository implementations, and external service adapters. The Host.MVC layer is the composition root with Areas (Admin, Customer, Identity) for role-based UI separation. Domain entities must have zero external dependencies — this is the Dependency Rule that makes the architecture work.

**Major components:**
1. **Domain Layer** — Core entities (Pet, Owner, Product, Order, MedicalRecord, AdoptionListing, LostPetReport), enums, repository interfaces, domain events. Zero framework dependencies.
2. **Application Layer** — Business logic services, DTOs, service interfaces, FluentValidation validators, Result\<T\> error handling, pagination helpers.
3. **Infrastructure Layer** — EF Core DbContext with Fluent API configurations, repository implementations, Identity/claims setup, email/file storage/payment adapters.
4. **Host.MVC Layer** — MVC Controllers, Razor Views, ViewModels, Areas (Admin/Customer/Identity), Program.cs composition root, middleware pipeline.
5. **Areas** — Admin (dashboard, user/product/order management), Customer (account, pets, orders), Identity (login, register, password reset).

### Critical Pitfalls

1. **Clean Architecture Over-Engineering** — The project template may drive teams to add MediatR, AutoMapper, generic repositories, and unit-of-work before real complexity warrants it. Prevention: start with plain service classes, DbContext through an interface (`IApplicationDbContext`), and hand-written mapping. Add abstractions only when pain of tight coupling exceeds cost of layering. Warning: if 80% of use cases are 3-line Add+SaveChanges operations, you're over-engineered.

2. **Cross-Tenant Data Leakage** — If shelters/vet clinics share the platform, missing `WHERE TenantId = @tid` filters expose sensitive data. Prevention: EF Core global query filters, scoped `ITenantContext`, automatic TenantId assignment in `SaveChangesAsync`, tenant-namespaced cache keys. Test with 3+ simulated tenants minimum. This must be designed in Phase 0 — retrofitting after data exists is a migration nightmare.

3. **N+1 Queries and EF Core Performance** — Pet platforms have deeply nested entities (Pet → Appointments → MedicalRecords → Vaccinations). Prevention: pagination on ALL list endpoints from day one (default page size 20), `AsNoTracking()` on all read-only queries, projection (`.Select()`) instead of `.Include()` chains for list views, composite indexes starting with TenantId.

4. **Building for Adopters Without Building for Shelters** — The classic marketplace cold-start problem. If shelters find the platform increases their workload, they won't participate and there's no supply for adopters. Prevention: every shelter-facing feature must reduce administrative burden; provide unified dashboard for listing management; interview shelter staff during requirements; offer free tier.

5. **Stale Pet Listings Destroy Trust** — 42.5% of shelter websites have stale listings; adoption inquiries are 340% higher with real-time sync. Prevention: local database as source of truth, auto-expiry logic, "Last updated" timestamps, shelter dashboard widget for stale listings, "Confirm still available" buttons.

## Implications for Roadmap

Based on combined research from all four files, suggested phase structure:

### Phase 0: Foundation & Project Setup
**Rationale:** Must come first — establishes the architecture, middleware pipeline, Identity system, and data access patterns that every subsequent phase depends on. Pitfalls research shows this phase must resist over-engineering while correctly implementing tenant isolation and middleware ordering.
**Delivers:** Solution structure (4 projects), EF Core DbContext with global query filters, ASP.NET Core Identity with 4 roles + claims-based permissions, middleware pipeline in correct order, Tailwind CSS v4 setup, seed data for roles/permissions.
**Addresses:** Authentication & roles (table stakes), responsive design foundation, admin dashboard foundation.
**Avoids:** Pitfall 1 (over-engineering — skip MediatR/AutoMapper/generic repos), Pitfall 2 (anaemic domain — establish private setters + business methods on first entity), Pitfall 3 (cross-tenant data leakage — design TenantId isolation before any data flows), Pitfall 4 (middleware ordering — get Program.cs right before any feature), Pitfall 6 (N+1 queries — establish pagination, AsNoTracking, projection patterns).

### Phase 1: Pet Profiles & Customer Accounts
**Rationale:** Pet Profiles are the second dependency after auth — e-commerce, adoption, lost pets, and medical records all center on pet data. The Pet entity model must be comprehensive from the start. Customer accounts provide user retention.
**Delivers:** Pet CRUD (multi-pet per owner, species/breed/age/weight/photos/health flags), customer My Account page, pet management UI.
**Addresses:** Pet profiles (table stakes), customer account management (table stakes).
**Avoids:** Pitfall 7 (build shelter/vet tools alongside adopter features, not after — shelter interview data should inform pet profile fields).

### Phase 2: E-Commerce Module
**Rationale:** E-commerce is the revenue model and has no dependencies on adoption, lost pets, or medical records. Research recommends validating one vertical before expanding. E-commerce is the safest first vertical because it doesn't require supply-side partnerships (shelters) or complex workflows (vet SOAP notes).
**Delivers:** Product catalog with search/filter, shopping cart (cookie/session-based), checkout flow, order management (customer + admin), Cash on Delivery payment.
**Addresses:** Product catalog + search + cart + checkout + order management (all table stakes).
**Avoids:** Pitfall 8 (over-expansion — prove unit economics in e-commerce before adding adoption/medical). Uses: Stripe.net (when ready), Inventory stored procedures for atomic stock updates.

### Phase 3: Adoption & Lost Pets
**Rationale:** Depends on Pet Profiles (Phase 1) and search infrastructure. These modules are independent of e-commerce (Phase 2) but share pet data and auth. Research emphasizes that shelter/vet tools must ship alongside adopter features.
**Delivers:** Adoption pet listings with search/filter, adoption application flow + shelter review, lost pet reporting + search + alerts, shelter dashboard for listing management, auto-expiry for stale listings.
**Addresses:** Adoption listings + search + application flow (table stakes), lost pet reporting + search (table stakes).
**Avoids:** Pitfall 5 (local DB as source of truth — no Petfinder API dependency as single point of failure), Pitfall 7 (build for shelters first — unified dashboard, digital applications), Pitfall 9 (auto-expiry, "Last updated" timestamps, stale listing alerts).

### Phase 4: Medical Records & Vet Dashboard
**Rationale:** Depends on Pet Profiles (Phase 1) and vet role from Phase 0. Medical records require structured SOAP data, which is more complex than standard CRUD. Research recommends deferring to after e-commerce validation.
**Delivers:** Basic medical records (vaccination history, medication records, visit notes), vet dashboard with patient search, pet health history view for owners.
**Addresses:** Medical records (table stakes), vet dashboard (differentiator — P2).
**Avoids:** Pitfall 6 (pagination, AsNoTracking on all vet dashboard queries — 500+ patient lists).

### Phase 5: Polish, Admin Dashboard & Notifications
**Rationale:** Final phase ties together operational tools and user retention features. Depends on all previous modules being in place for admin to manage.
**Delivers:** Full admin dashboard (metrics, analytics, role management), vaccination/medication reminders, emergency pet passport, notification system (order confirmations, adoption status updates), order confirmation emails.
**Addresses:** Admin dashboard (table stakes), vaccination reminders (differentiator), emergency pet passport (differentiator), order confirmations (expected).

### Phase Ordering Rationale

- **Auth first:** Everything depends on authentication and roles — it's the root dependency in the feature tree
- **Pet profiles second:** Central entity that e-commerce (product filtering), adoption, lost pets, and medical records all reference
- **E-commerce before adoption:** Revenue model first; doesn't require shelter partnerships or complex multi-party workflows
- **Adoption before medical records:** Adoption has broader user impact; medical records require vet-specific SOAP workflows that need more design input
- **Notifications last:** Depends on all modules existing to send meaningful alerts
- **This order avoids Pitfall 8:** Validates unit economics in e-commerce (Phase 2) before expanding to adoption (Phase 3) and medical records (Phase 4)

### Research Flags

Phases likely needing deeper research during planning:
- **Phase 0:** Multi-tenant architecture patterns — tenant resolution middleware, EF Core global query filters, composite index strategy with TenantId leading column
- **Phase 2:** Payment integration — Stripe Checkout vs embedded, idempotency keys, webhook handling, refund flows in admin
- **Phase 3:** Adoption workflow — shelter review process, application screening criteria, Petfinder API sync architecture (outbox pattern, background sync), stale listing detection algorithms
- **Phase 4:** Vet SOAP workflow — structured note format, prescription management, appointment scheduling, medical record access control (ownership-scoped, not just tenant-scoped)

Phases with standard patterns (skip research-phase):
- **Phase 1:** Pet CRUD and customer accounts — straightforward ASP.NET MVC + EF Core patterns, well-documented
- **Phase 5:** Admin dashboard, notifications — established patterns for ASP.NET Core MVC admin panels and background job processing

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Stack | HIGH | All technologies verified against official sources (.NET 10 download page, EF Core releases, Tailwind v4 docs, Stripe.net NuGet). Version compatibility matrix confirmed. |
| Features | MEDIUM-HIGH | Feature landscape derived from 12+ competitor analyses and 6+ industry guides. MVP definition aligns with multiple successful platforms. Anti-feature list grounded in pet startup post-mortems. |
| Architecture | HIGH | 4-layer Clean Architecture validated by Microsoft eShopOnWeb, VetCare (vet SaaS), and KeystoneCommerce (e-commerce). Patterns (Repository+UoW, Result\<T\>, Area-based MVC) are well-documented and proven. |
| Pitfalls | MEDIUM | 9 critical pitfalls identified from real pet startup failures (Vetster, Fuzzy, BarkBox, PetBacker) and .NET architecture anti-patterns. Multi-tenant isolation guidance comes from vet care specific sources. Some pitfalls (cross-tenant leakage) are inferred rather than directly experienced. |

**Overall confidence:** HIGH

### Gaps to Address

- **Arabic RTL validation:** Tailwind v4's `rtl:` variant support is documented but has limited MVC-specific examples. Verify RTL layout works correctly with ASP.NET Core Areas during Phase 0.
- **Shelter/vet workflow requirements:** Pitfall 7 emphasizes that shelter staff must be interviewed during Phase 1 (requirements). No existing research captures their actual workflows — this is a critical gap for Phase 3 planning.
- **Multi-tenant vs single-tenant:** The research assumes multi-tenancy based on the platform serving multiple shelters/vet clinics, but PROJECT.md doesn't explicitly mandate multi-tenancy. Clarify during Phase 0 planning — if single-tenant, Pitfall 3 is less critical but still good practice.
- **MediatR necessity:** Research recommends skipping MediatR initially (Pitfall 1) but the PROJECT.md Clean Architecture constraint may create pressure to include it. The decision should be explicit: start without MediatR, add only when CQRS separation proves necessary.
- **Background job infrastructure:** No research on Hangfire vs built-in .NET 8+ background services vs custom implementation. Needed for Phase 5 (notifications, reminders, email).
- **Image/file storage strategy:** Local filesystem initially (per ARCHITECTURE.md), but no research on when to migrate to blob storage or how to handle image uploads for pet photos at scale.

## Sources

### Primary (HIGH confidence)
- Microsoft .NET 10 Download Page — SDK 10.0.301, ASP.NET Core Runtime 10.0.9
- EF Core Releases — EF Core 10.0.10 latest stable, LTS until Nov 2028
- Microsoft eShopOnWeb — Official Clean Architecture reference for ASP.NET Core
- VetCare (github.com/paulo-raoni/vetcare) — Multi-tenant vet SaaS on .NET 8 Clean Architecture
- KeystoneCommerce (github.com/Zyad-Eltayabi/KeystoneCommerce) — ASP.NET Core MVC e-commerce with Clean Architecture
- Tailwind CSS v4 ASP.NET Core 10 MVC — @source directive, MSBuild integration
- ASP.NET Core Identity Claims Auth — Claims-based and policy-based authorization
- Stripe.net NuGet — v48.3.0 stable, .NET 8+ compatible

### Secondary (MEDIUM confidence)
- Pet platform startup post-mortems (Vetster, Fuzzy, BarkBox, PetBacker, Zumvet) — Unit economics and over-expansion failures
- LOW/CODE pet adoption marketplace analysis — "built for adopters without building for shelters first"
- Social Animal 200-shelter audit — 42.5% stale listings, 340% adoption inquiry difference with real-time sync
- Bhaw Bhaw Pet Marketplace case study — Modular architecture, commerce system, service booking
- Pawlly Pet Services SaaS — 31-module pet services marketplace
- Zulbera Marketplace Guide — MVP components, recommended build sequence

### Tertiary (LOW confidence — needs validation)
- PrepStack Clean Architecture guidance — "if you cannot name three real business rules in Domain, you don't need a Domain layer yet" (heuristic, not verified against production systems)
- AutoMapper license change at v13 — v16.2.0 free for non-enterprise; verify against current license terms for any commercial use
- Tailwind v4 RTL support — documented but limited MVC-specific examples; needs hands-on verification

---
*Research completed: 2026-07-18*
*Ready for roadmap: yes*
