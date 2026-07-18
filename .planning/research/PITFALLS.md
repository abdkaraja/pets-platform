# Pitfalls Research

**Domain:** Pet Platform Web Application (ASP.NET Core MVC, Clean Architecture)
**Researched:** 2026-07-18
**Confidence:** MEDIUM

## Critical Pitfalls

### Pitfall 1: Clean Architecture Over-Engineering on Simple CRUD

**What goes wrong:**
Teams adopt a full Clean Architecture template (4+ projects, MediatR, AutoMapper, generic repositories, unit-of-work, specification pattern) for what is fundamentally a CRUD application with thin business logic. The result is 4 files per endpoint, 3 mapping layers, and a "domain" that is just data classes with public getters/setters and no behavior. Every feature takes 3x longer than it should.

**Why it happens:**
Pet platforms have straightforward domain rules (inventory math, adoption status transitions, appointment scheduling). Developers copy enterprise templates without asking whether the complexity is warranted. The PROJECT.md mandates Clean Architecture with 4 layers, making this pitfall especially dangerous because the team may feel compelled to over-engineer.

**How to avoid:**
- Implement the Dependency Rule (dependencies point inward) but skip MediatR, generic repositories, AutoMapper, and unit-of-work until a real problem demands them.
- Use `IApplicationDbContext` interface to give the Application layer EF Core access without a repository wrapper.
- Keep mapping to TWO layers max: Domain entity (for writes) and ViewModel/DTO (shaped for the UI). Use manual mapping or a lightweight mapper.
- Follow PrepStack's rule: "If you cannot name three real business rules that belong in the Domain layer, you probably do not need a Domain layer yet."
- Start with a single ASP.NET Core project organized by feature folders. Refactor toward Clean Architecture boundaries when the pain of tight coupling exceeds the cost of layering.

**Warning signs:**
- 80% of "use cases" are 3-line Add + SaveChanges operations
- Entities have no methods, only properties
- Moving between layers consumes more time than writing features
- AutoMapper profiles are identical to the EF entity shape
- Program.cs exceeds 100 lines of DI registrations

**Phase to address:**
Phase 0 (foundation) — resist over-engineering from the start. The project template should be deliberately boring: plain service classes, DbContext through an interface, hand-written mapping methods. Add MediatR or CQRS only when a use case in Phase 2+ genuinely benefits from it.

---

### Pitfall 2: Anaemic Domain Model with Logic in Controllers

**What goes wrong:**
Business rules for adoption eligibility, appointment scheduling conflicts, inventory thresholds, and medical record access permissions end up in controllers or application services rather than on domain entities. The domain layer becomes a data bag with public setters. Controllers grow fat, tests require mocking everything, and rule changes scatter across the codebase.

**Why it happens:**
ASP.NET Core MVC encourages the "thin controller, fat service" pattern organically. Developers default to putting logic in services because it's the path of least resistance. The Clean Architecture constraint makes it worse — entities feel like "just models" while services feel like "where the real code goes."

**How to avoid:**
- Enforce private setters on domain entities. The only way to change state is through entity methods (e.g., `pet.MarkAsAdopted()`, `appointment.Reschedule(newTime)`) that enforce invariants.
- Put validation rules on the entity, not in a service validator. `appointment.OverlapsWith(existingAppointments)` belongs on Appointment, not in a service.
- Use factory methods (`Pet.RegisterForAdoption(...)`) instead of public constructors + property setting.
- Controllers should only: parse input, call a service, map result to view model, return response. Nothing more.

**Warning signs:**
- Controllers exceeding 60 lines
- Domain entities with all public get/set and zero methods
- `if` statements about business rules inside controller actions
- Domain project compiles with zero test coverage because "there's nothing to test"

**Phase to address:**
Phase 0 (domain layer design) — establish the pattern before any feature work. Create the first entity (e.g., Pet) with private setters, factory methods, and invariants. Use it as the template for all later entities.

---

### Pitfall 3: Cross-Tenant Data Leakage (Multi-Tenant Isolation Failure)

**What goes wrong:**
A pet platform serves multiple shelters, vet clinics, or pet owners. A missing `Where(t => t.TenantId == currentTenant)` in a query, a background job without tenant context, a raw SQL report that bypasses EF Core filters, or a cache miss on tenant-scoped keys — any of these can expose Shelter A's adopters, Vet B's patient records, or Owner C's medical data to another tenant. In staging with one tenant, the bug is invisible. In production, it's a data leak.

**Why it happens:**
Multi-tenancy is designed at the database level (shared schema with TenantId columns) but enforcement is manual. Developers forget filters. Background jobs run outside HTTP request scope and lose tenant context. Caching is implemented without tenant-aware keys. Raw SQL bypasses EF Core global query filters entirely.

**How to avoid:**
- Use EF Core global query filters as the primary isolation mechanism — they automatically add `WHERE TenantId = @tid` to every query without developer action.
- Inject a scoped `ITenantContext` into the `DbContext`; capture the context object reference (not its current value) in the filter lambda.
- Override `SaveChangesAsync` to automatically assign `TenantId` on insert — make it impossible to create a record without one.
- For background jobs: store `TenantId` in the job payload. The handler must restore tenant context before accessing data.
- Cache keys MUST be namespaced by tenant: `$"Tenant:{tenantId}:CacheKey"`.
- Raw SQL in reports must explicitly include `TenantId` conditions — document every bypass with a code comment.
- Composite indexes must start with `TenantId` — without this, every query is a full table scan at scale.
- Integration tests must verify Tenant A cannot read Tenant B's data. Run tests with multiple simulated tenants.

**Warning signs:**
- Any query using `IgnoreQueryFilters()` without manually re-adding tenant conditions
- Background job handlers that don't reference `ITenantContext`
- Cache keys without tenant prefix
- Raw SQL strings in Infrastructure that lack `WHERE TenantId =`
- Staging environments with only one tenant (test with 3+ tenants minimum)

**Phase to address:**
Phase 0 (infrastructure setup) — multi-tenancy must be designed and tested before any data flows. Retrofitting tenant isolation after data exists is a migration nightmare.

---

### Pitfall 4: ASP.NET Core Middleware Ordering That Breaks Auth/Security

**What goes wrong:**
`UseAuthorization` is placed before `UseAuthentication`, so the user principal is never populated and all auth checks return 401. Exception handling middleware is registered too late and doesn't catch exceptions from routing/endpoints. CORS middleware is placed after auth middleware, so preflight requests that fail auth never get CORS headers. Static file middleware is placed after routing, wasting CPU on requests that could short-circuit. The result is subtle security bugs that only surface in production behind a reverse proxy.

**Why it happens:**
ASP.NET Core's middleware pipeline is a chain where order determines behavior. Developers copy-paste middleware order from tutorials or templates without understanding the semantics. The pet platform uses Razor Views + jQuery (server-rendered), which means cookie auth and anti-forgery tokens add additional ordering constraints.

**How to avoid:**
Follow this canonical middleware order (from Microsoft's documentation, verified against current .NET LTS):

```
// 1. Exception handling (FIRST — catch everything)
if (env.IsDevelopment()) app.UseDeveloperExceptionPage();
else { app.UseExceptionHandler("/error"); app.UseHsts(); }

// 2. HTTPS redirect
app.UseHttpsRedirection();

// 3. Static files (BEFORE routing — short-circuit early)
app.UseStaticFiles();

// 4. Routing
app.UseRouting();

// 5. CORS (BETWEEN UseRouting and UseAuthentication)
app.UseCors("PolicyName");

// 6. Authentication (BEFORE Authorization)
app.UseAuthentication();

// 7. Authorization (AFTER Authentication)
app.UseAuthorization();

// 8. Anti-forgery (custom middleware, AFTER auth)
// 9. Tenant resolution middleware
// 10. Endpoints
app.MapControllers();
```

- Register custom middleware (tenant resolution, request logging, anti-forgery) at the correct layer relative to auth.
- Use middleware, not filters, for cross-cutting concerns that must run on every request.

**Warning signs:**
- 401 errors that persist even with valid credentials
- CORS errors in browser console that appear random
- Stack traces from ASP.NET Core internals reaching the client in production
- `UseAuthorization` called without `UseAuthentication` before it

**Phase to address:**
Phase 0 (host configuration in Program.cs) — get the middleware pipeline right during initial project setup. It is cheap to fix now, expensive to debug later.

---

### Pitfall 5: Petfinder/Third-Party API Dependency as a Single Point of Failure

**What goes wrong:**
The pet adoption or lost-pets module integrates with Petfinder, Adopt-a-Pet, or RescueGroups for listing syndication. When Petfinder's API goes down (as happened in Dec 2025 — lasting months, preventing shelters from uploading photos, dropping adoption inquiries by 95%), the platform's core feature is non-functional. The platform has no fallback, no cache, and no way for shelters to manage listings independently. The app is a thin wrapper around someone else's service.

**Why it happens:**
Third-party APIs offer quick feature velocity. Developers integrate deeply (pet listings, photos, application forms all flow through the external API) without designing for API failure modes. The pet platform's PROJECT.md doesn't mention third-party dependencies explicitly, making this an unexamined risk.

**How to avoid:**
- Design the pet listing system with its own database schema as the single source of truth. Third-party APIs are sync targets/discovery channels, not data backends.
- Implement an sync service (cron or background job) that pushes local data TO Petfinder, Adopt-a-Pet, etc., and pulls listings FROM them into a local cache.
- When APIs are down, fall back to local cached data with a "last updated" banner. The platform should never show a 404 or empty state because Petfinder is slow.
- Use the Outbox pattern for outbound sync — persist the sync message in a local table, dispatch from a background worker. Never sync in the request path.
- Monitor API health and alert when third-party syndication falls behind.

**Warning signs:**
- Pet listings are fetched from Petfinder API on every page load (no local cache)
- Shelter staff cannot update listings when Petfinder is down
- The platform has no local pet database — it's a pure API proxy
- No retry logic, no circuit breaker, no fallback content

**Phase to address:**
Phase 3 (adoption/lost-pets module) — the data model and sync architecture must be designed in Phase 0 (foundation). The local pet entity is a core domain concept that predates any third-party integration.

---

### Pitfall 6: N+1 Queries and EF Core Performance Traps at Pet Platform Scale

**What goes wrong:**
The pet listing page loads 50 pets. For each pet, EF Core lazily loads breed, photos, medical records, and adoption status — 201 queries for one page view. The vet dashboard loads all patients without pagination. The admin dashboard loads every pet, adopter, and transaction into a single view. At 100 users the app is fine. At 1,000 users the database CPU hits 100%. At 10K users the app is unusable.

**Why it happens:**
Pet platforms have deeply nested entities (Pet → Appointments → MedicalRecords → Vaccinations; Pet → AdoptionApplications → Adopter → HomeChecks). Developers use `.Include()` chains without selectivity, fail to call `AsNoTracking()` on read-only queries, load full entities when only a summary is needed, and skip pagination because "we only have 50 pets right now."

**How to avoid:**
- Every list endpoint MUST have pagination from day one (page-based for small sets, cursor-based for large feeds). Default page size of 20, max of 100.
- Use `AsNoTracking()` on ALL read-only queries. This is the default for queries — only omit it when you need change tracking.
- Use projection (`.Select(x => new PetSummaryDto {...})`) instead of `.Include()` chains for list views. Load only the columns the view needs.
- For dashboards, use a dedicated read model (denormalized table or in-memory cache) rather than querying the OLTP schema.
- Add composite indexes with TenantId as the leading column on all high-traffic tables.
- Create an EF Core interceptor that warns when a single request generates more than 5 database queries (N+1 detection).
- Use compiled queries for the most-called read paths (e.g., pet detail page, user profile).

**Warning signs:**
- Page load times increase linearly with record count
- SQL Server shows high CPU from repeated identical queries
- Entity Framework logging reveals hundreds of queries per request
- Any endpoint without explicit `.Take()` / `.Skip()` or `AsNoTracking()`
- Linq queries inside `foreach` loops

**Phase to address:**
Phase 0 (repository / data access layer) — establish pagination, AsNoTracking, and projection patterns before any feature creates queries. Every phase should have a performance review gate.

---

### Pitfall 7: Building the Platform for Adopters Without Building for Shelters/Vets First

**What goes wrong:**
The pet adoption feature focuses entirely on the adopter experience: beautiful pet galleries, smooth application flow, mobile-first design. But shelters find the platform adds to their workload — they have to manually update listings in Petfinder AND the platform's database, adoption applications arrive as PDFs they must re-enter, and there's no dashboard to track inquiries. Shelters stop listing. Without supply, there's no demand. The platform stalls.

**Why it happens:**
This is the classic marketplace cold-start problem. Most pet platforms fail not because of technology but because "they were built for adopters without building for shelters first" (per the LOW/CODE pet adoption marketplace analysis). Shelters are understaffed, running on spreadsheets and ad-hoc tools. If the platform increases their workload, they won't participate.

**How to avoid:**
- Every shelter/vet-facing feature must answer: "Does this reduce their administrative burden or increase it?"
- Provide a unified dashboard where shelters manage all platforms (Petfinder sync, website listings, application tracking) from one place.
- Auto-sync pet statuses across platforms — when a pet is adopted, it should update everywhere within hours, not weeks.
- Digital adoption forms that shelters can review, approve/reject, and e-sign within the platform (no PDFs, no emails).
- Offer a free tier for shelters — they have no budget for software.
- Interview shelter staff directly during Phase 1 (requirements). Don't assume you know their workflow.

**Warning signs:**
- Shelter/vet features are an afterthought in sprint planning
- "Shelters can just update Petfinder and we'll sync from there" (no — that's extra work for them)
- Adoption applications arrive as email attachments
- No way for a shelter to see all their active applications in one view
- Shelters are not represented in user research

**Phase to address:**
Phase 1 (discovery/interview) — shelter workflow understanding must precede Phase 3 (adoption module). Build shelter tools in Phase 3 alongside adopter features, not after.

---

### Pitfall 8: Over-Expansion and Unit Economics Ignorance

**What goes wrong:**
The platform launches with e-commerce, adoption, lost pets, and medical records simultaneously. Marketing spend is high. CAC exceeds LTV. The vet telemedicine feature relies on pandemic-era demand patterns. The marketplace grows to multiple cities before unit economics are proven in one. When funding dries up, the fixed-cost base (hosting, staff, warehouses) can't shrink fast enough. The platform collapses.

**Why it happens:**
The pet industry is emotionally compelling. Founders fall in love with the vision and skip the hard questions: "What does it cost to acquire a customer?" "What is their lifetime value?" "What happens when the pandemic pet boom fades?" The pet startup autopsy data is brutal: BarkBox ($1.6B → $1.20/share), Vetster ($63M raised → acquired at 60% below B-round), Fuzzy ($80.5M → shutdown with no notice), PetBacker ($2.5M → 40% revenue recovery → shutdown), Zumvet ($4.8M → disappeared overnight). Every one of these failed on unit economics.

**How to avoid:**
- Build one feature well before adding the next. The PROJECT.md already lists 6+ feature areas — this is a warning sign, not a strength.
- Validate unit economics in ONE market before expanding. Vetster's Mark Bordo: "build supply before demand" — but even they couldn't escape pandemic normalization.
- Model pessimistic scenarios: "What if post-pandemic demand drops 40-60%?" Vetster didn't, and it killed them.
- Keep a lean fixed-cost base. Outsource fulfillment. Use shared hosting until traffic proves dedicated infrastructure.
- Track cohort-level metrics (not just aggregate): subscriber count over 3+ consecutive quarters of decline = structural, not seasonal.
- Do not raise at peak valuation during a tailwind. "SPAC valuations built on pandemic-era subscriber curves ignore mean reversion."

**Warning signs:**
- Revenue grows but losses grow faster
- Multiple features are "in development" before any one is profitable
- Marketing spend as a percentage of revenue exceeds 50%
- No clear path to breakeven at current burn rate
- "We'll figure out the business model later" mentality

**Phase to address:**
Phase 1 (MVP scoping) — the ROADMAP must prioritize one vertical (e.g., e-commerce) and prove unit economics before expanding to adoption, lost pets, and medical records. This is a product strategy decision, not an engineering one, but the project structure must enforce it.

---

### Pitfall 9: Stale Pet Listings Destroy Trust

**What goes wrong:**
Pet listings on the platform show animals that were adopted weeks ago. Users apply for pets that are no longer available. They drive to a shelter that no longer has the animal. 42.5% of shelter websites in a 200-site audit had listings over 30 days stale. Adoption inquiry volume for sites with real-time sync was 340% higher than those without. Stale listings are the #1 reason potential adopters give up.

**Why it happens:**
Pet statuses change constantly (adopted, fostered, medical hold, returned). If the platform relies on manual updates ("the shelter will mark it as adopted"), it won't happen — shelters are too understaffed. If the platform syncs from Petfinder/adoption APIs, those APIs have their own latency (RescueGroups reports 48+ hour delays in data propagation).

**How to avoid:**
- The platform's database is the single source of truth. All listing updates go through the local schema first.
- Implement a "Pending" badge on animals with active applications — this creates urgency and signals transparency.
- Provide a shelter dashboard widget showing "listings not updated in 7+ days" to prompt action.
- For the lost-pets module: auto-expire listings after 30 days unless the owner confirms the pet is still missing.
- Fall back gracefully: show a "Last updated: 2 hours ago" timestamp rather than hiding stale data.
- During Phase 3 (adoption), enforce: no listing goes live without auto-expiry logic.

**Warning signs:**
- Pet detail pages without "Last updated" timestamps
- No mechanism for shelters to bulk-update listing statuses
- Adopted pets still visible in search results
- Users report: "I applied but the pet was already gone"

**Phase to address:**
Phase 3 (adoption/lost-pets module) — auto-expiry, status sync, and freshness indicators must be designed as core features, not afterthoughts.

---

## Technical Debt Patterns

Shortcuts that seem reasonable but create long-term problems.

| Shortcut | Immediate Benefit | Long-term Cost | When Acceptable |
|----------|-------------------|----------------|-----------------|
| AutoMapper for ALL entity<->DTO mapping | Fast initial mapping | Mystery mappings that break at runtime; compilation errors deferred to runtime; 500+ line profiles | Never — use manual mapping or source generators (Mapperly). AutoMapper is tech debt from commit 1. |
| Generic `IRepository<T>` for every entity | "Reusable" data access code | No repository is actually generic — each aggregate has different query needs; N+1 enabled because `IRepository<T>` exposes `IQueryable` | For true CRUD entities (lookup tables, audit logs). NOT for aggregates like Pet, Order, Appointment. |
| Sync queries (`.ToList()` instead of `ToListAsync()`) | "Works fine on my machine" | Thread pool starvation at scale; request queueing; ASP.NET Core throughput collapse | **Never** — ASP.NET Core runs async-first. Sync calls block the thread pool. |
| No pagination on list endpoints | "We only have 50 records" | At 500 records: page load slows. At 5K: database hit. At 50K: timeout. | **Never** — add pagination from day one. Default page size 20, max 100. |
| Hardcoded connection strings in appsettings.json | Fast setup | Secret leaked to git; different configs across environments; no rotation | **Never** — use User Secrets in dev, environment variables in production. |
| Storing images in the database (byte arrays) | "Everything in one place" | Database bloat; slow backups; CDN impossible; cache-unfriendly | **Never** — store files on disk or S3/compat, store paths in DB. |
| Single DbContext class for everything | Simple, one class | As features grow, the DbContext becomes a 2000-line file; tenant filters get complex; migrations slow down | Acceptable for Phase 0-1. Split by bounded context in Phase 2+ (e.g., EcommerceDbContext, AdoptionDbContext). |
| Skipping integration tests | "Unit tests are enough" | Multi-tenant data leaks undetected; EF Core query behavior unverified; middleware ordering bugs invisible | **Never** for tenant isolation and data access tests. Unit tests alone cannot verify tenant boundaries. |

---

## Integration Gotchas

Common mistakes when connecting to external services.

| Integration | Common Mistake | Correct Approach |
|-------------|----------------|------------------|
| Petfinder API | Calling Petfinder API on every page load; no local cache | Local pet database as source of truth; Petfinder is a sync destination and secondary discovery channel only |
| Payment processor (Stripe) | Calling payment API synchronously in the request path; no idempotency key | Use Stripe's idempotency keys; dispatch payment confirmation to a background handler via outbox pattern |
| Email/SMS (vet reminders, adoption follow-ups) | Sending from the request handler; no retry logic | Queue notifications in the database outbox; background worker sends with exponential backoff and dead-letter after max retries |
| Image storage (pet photos) | Storing images on the web server's local disk | Use S3-compatible storage (or Azure Blob, MinIO for dev); serve via CDN; generate thumbnails asynchronously |
| Shelter management software (ShelterLuv, PetPoint) | Building a direct integration to each one | Design an integration abstraction layer (e.g., `IShelterDataProvider`); implement one adapter per backend; use the Outbox pattern for sync |
| Identity Provider (if not using ASP.NET Core Identity) | External auth token stored without local session; every request re-validates against IdP | Use ASP.NET Core Identity as the local identity store; external tokens are for login only; session is local |

---

## Performance Traps

Patterns that work at small scale but fail as usage grows.

| Trap | Symptoms | Prevention | When It Breaks |
|------|----------|------------|----------------|
| No pagination on list endpoints | Page load time grows linearly with row count | Default pagination on ALL list endpoints from day one | ~500 records |
| Eager-loading entire entity graphs with `.Include()` | Large SQL queries with 8+ joins; slow serialization | Use `.Select()` projection for read paths; load only columns the view needs | ~50 records with 3+ navigation properties |
| Missing `AsNoTracking()` on read-only queries | EF Core tracking memory grows with each request; CPU increases | Always call `AsNoTracking()` on queries that don't mutate data | ~1,000 entities tracked per request |
| No index on TenantId column | Full table scans on every multi-tenant query; database CPU spikes | Add composite indexes starting with TenantId on every tenant-scoped table | ~10K rows per tenant |
| Not using compiled queries for hot paths | EF Core query compilation overhead on every request | Use compiled queries for pet detail page, user profile, dashboard aggregates | ~100 requests/second |
| Loading full entity objects for dashboards | Dashboard queries return 50+ columns when 4 are needed | Use dedicated read models/DTOs for dashboard data | ~5K rows |
| Session state in-process (not distributed) | Session lost on app restart; cannot scale to multiple instances | Use SQL Server or Redis for session state | 2+ web server instances |
| Background jobs without idempotency | Duplicate emails, double charges, duplicate adoption records | Every job handler must be idempotent — use a processed-events table for dedup | Any retry scenario |

---

## Security Mistakes

Domain-specific security issues beyond general web security.

| Mistake | Risk | Prevention |
|---------|------|------------|
| Missing tenant isolation on vet medical records | Vet A sees Vet B's patient records | EF Core global query filters on TenantId; integration tests verify cross-tenant isolation |
| Adoption application form accepts file uploads with no validation | Malware uploaded via "vet reference letter" field | Validate file types (PDF, images only); scan with antivirus; store outside web root; serve via separate endpoint with auth check |
| Lost-pets module exposes reporter's phone/email publicly | Harassment, spam, doxxing of users reporting found pets | Use in-app messaging for contact; never expose personal contact info publicly; use temporary anonymous relay |
| Admin dashboard accessible without IP restriction | Brute force against admin accounts; data exfiltration of all tenants | Admin routes under IP whitelist or VPN; rate-limit admin login attempts; audit all admin actions |
| Pet medical records accessible without owner authorization | Medical data of Owner A's pet visible to Owner B | Enforce ownership check on every medical record endpoint: `pet.OwnerId == currentUser.Id` AND `pet.TenantId == currentTenant.Id` |
| Payment card data logged in plain text | PCI DSS violation; card data in logs and error reports | Never log raw payment data; use Stripe Elements or similar to avoid touching card data entirely |
| Anti-forgery token missing on AJAX forms in Razor Views | CSRF attacks on adoption forms, order submissions, profile updates | Add `@Html.AntiForgeryToken()` to all POST forms; validate with `[ValidateAntiForgeryToken]` attribute; configure jQuery AJAX to include the token header |
| Soft-delete bypassed for GDPR right-to-erasure | User data persists after deletion request | Separate hard-delete operations for compliance (GDPR/CCPA erasure) that are explicit, logged, and audited. Normal application workflow uses soft-delete. |

---

## UX Pitfalls

Common user experience mistakes in this domain.

| Pitfall | User Impact | Better Approach |
|---------|-------------|-----------------|
| Pet search with no filters (breed, age, size, temperament, location) | Users scroll through hundreds of irrelevant listings | Implement faceted search with breed, age range, size, temperament, adoption status, distance filters |
| Adoption form requires desktop browser | Mobile users (60-78% of traffic) abandon at the form | Mobile-first responsive design; accept multi-page form submissions across devices |
| No "Pending" badge on active applications | Multiple adopters apply for the same pet; first applicant's effort wasted | Show application count ("3 applications pending") to signal demand; hold the pet for the first qualified applicant for 24h |
| No "Last updated" timestamp on pet listings | Users fall in love with pets that were adopted weeks ago | Prominent "Last updated: 2 hours ago" on every listing; auto-expire or grey out listings > 7 days stale |
| Vet dashboard shows all patients without search | Vets with 500+ patients scroll endlessly | Search by patient name, owner name, date of last visit; filter by species, status, upcoming appointments |
| Lost-pet report requires creating an account | Urgent user (just lost their pet) abandons the form | Allow lost-pet reports with just name, phone, and email; no account required; create account as follow-up prompt |
| Error messages in English on Arabic-first platform | Arabic users can't understand validation errors | Localize ALL user-facing strings to Arabic (project requirement: Arabic only for v1). Use .NET resources files |
| Overnight processing of adoption applications | Adopter loses momentum between application and response | Target 2-3 day processing (shelter-side); send automated status updates at every stage; show expected timeline |

---

## "Looks Done But Isn't" Checklist

Things that appear complete but are missing critical pieces.

- [ ] **Pet listing page:** Often missing "Last updated" timestamp, auto-expiry for adopted pets, and a clear "adoption pending" badge — verify all three exist per listing.
- [ ] **Multi-tenant isolation:** Often missing on background jobs, cache keys, raw SQL reports, and admin dashboards — verify tenant context propagates through every data access path.
- [ ] **Admin dashboard:** Often missing IP restriction, rate limiting, action audit trail, and tenant-scoped views — verify admin can only see their own tenant's data.
- [ ] **Adoption application form:** Often missing CAPTCHA, CSRF protection, mobile-responsive layout, application status tracking (submitted → reviewed → interview → approved/denied), and save-as-draft — verify all six.
- [ ] **Middleware pipeline:** Often missing correct ordering (exception handling first, CORS between routing and auth, static files before routing) — verify the exact order from Pitfall 4.
- [ ] **EF Core query patterns:** Often missing `AsNoTracking()`, pagination, projection instead of `.Include()`, and composite indexes — verify all four on every list endpoint.
- [ ] **Background jobs:** Often missing tenant context, idempotency, exponential backoff, dead-letter queue, and monitoring — verify all five on every background job path.
- [ ] **Error handling:** Often missing localized error messages (Arabic), user-friendly error pages instead of yellow screens, and structured error logging with TenantId — verify all three.
- [ ] **Payment flow:** Often missing idempotency keys, Stripe webhook idempotency, refund path in admin dashboard, and transaction records that survive webhook delivery failures — verify all four.
- [ ] **Pet medical records:** Often missing ownership-scoped access (not just tenant-scoped), audit log of who viewed records, and export capability for owners — verify all three.

---

## Recovery Strategies

When pitfalls occur despite prevention, how to recover.

| Pitfall | Recovery Cost | Recovery Steps |
|---------|---------------|----------------|
| Cross-tenant data leak discovered in production | HIGH — legal liability, PR crisis, potential loss of tenants | 1. Isolate the affected tenant(s) by blocking access. 2. Determine scope of leaked data (audit logs + DbContext query log replay). 3. Notify affected tenants per contractual/compliance obligations. 4. Deploy fix (add missing tenant filter). 5. Add integration test proving cross-tenant isolation. 6. Schedule penetration test. |
| Clean Architecture over-engineering slowing all features | MEDIUM — refactoring cost, no runtime risk | 1. Identify which abstractions have exactly one implementation and never change. 2. Delete the interface, inline the implementation. 3. Remove MediatR if handlers are thin wrappers around services. 4. Remove AutoMapper, replace with manual mapping. 5. Consolidate projects if Layer boundaries don't provide value. |
| Petfinder API down, platform shows no pets | HIGH — core feature non-functional, users leave | 1. Serve stale cached data with "Last updated" banner. 2. Add local pet database if one doesn't exist. 3. Implement a background sync worker. 4. Fall back to shelter-provided Petfinder widget if local data is also stale. |
| N+1 queries causing DB CPU at 100% | HIGH — production incident | 1. Kill the offending queries (restart app if needed). 2. Add missing `.Include()` or projection. 3. Add `AsNoTracking()`. 4. Add a query-count interceptor to prevent recurrence. |
| Stale pet listing: user drives to shelter, pet is gone | LOW/MEDIUM — reputational damage, user frustration | 1. Add prominent "last updated" timestamp. 2. Auto-expire listings after configurable days. 3. Send shelter a weekly email: "You have X listings not updated in 7+ days." 4. Add "Confirm still available" button to shelter dashboard. |
| Background job sends duplicate adoption confirmation | MEDIUM — user confusion, potential reputational damage | 1. Add idempotency key tracking (processed_events table). 2. Add dedup check before sending. 3. Monitor duplicate events to find root cause (outbox replay? queue redelivery?). |

---

## Pitfall-to-Phase Mapping

How roadmap phases should address these pitfalls.

| Pitfall | Prevention Phase | Verification |
|---------|------------------|--------------|
| Clean Architecture over-engineering | Phase 0 (foundation) | Code review: no MediatR, no generic repositories, no AutoMapper in the project template |
| Anaemic domain model | Phase 0 (domain layer) | Domain entities have private setters and business methods; controllers < 60 lines |
| Cross-tenant data leakage | Phase 0 (infrastructure) | Integration tests with 3+ simulated tenants prove no data leaks |
| Middleware ordering breaks auth | Phase 0 (Program.cs) | Automated middleware order test; manual review against canonical order |
| Third-party API single point of failure | Phase 3 (adoption module) | Local DB as source of truth; Petfinder sync is background-only |
| N+1 queries and EF Core performance | Phase 0 (data access patterns) | Every list endpoint has pagination + AsNoTracking + projection |
| Building for adopters without shelters | Phase 1 (discovery) + Phase 3 | User research includes shelter staff interviews; shelter dashboard ships alongside adopter features |
| Over-expansion / unit economics | Phase 1 (roadmap/RFP) | ROADMAP limits Phase 2 to ONE feature vertical; business case shows path to breakeven |
| Stale pet listings | Phase 3 (adoption module) | Auto-expiry feature exists; "last updated" on all listings; shelter dashboard shows stale listings |
| Anti-forgery token missing on AJAX | Phase 0 (Razor layout) | Every POST form has anti-forgery token; automated test verifies CSRF protection |
| No pagination on list endpoints | Phase 0 (repository layer) | Every `GetAll` / list method has mandatory `.Take()` / `.Skip()` parameters |
| Missing TenantId on cache keys | Phase 0 (caching layer) | Cache abstraction automatically prefixes keys with tenant ID |
| Background jobs without tenant context | Phase 2+ (background workers) | Every job payload includes TenantId; handler restores tenant context before DB access |

---

## Sources

- **Pet platform startup post-mortems:** Fuzzy ($80.5M shutdown, midnight-comm.com), Vetster ($63M raised → 60% valuation haircut, unicornburn.com), BarkBox ($1.6B→$1.20/share, unicornburn.com), PetBacker (shutdown after pandemic revenue collapse, unicornburn.com), The Vets (shutdown with ongoing billing to customers, VIN News), Zumvet (shutdown with no notice, ongoing subscription charges, Maxthon blog), Wiggles (overexpansion, runway 2 months, The Real Preneur)
- **Clean Architecture anti-patterns:** PrepStack, Code With Vane (Medium), Stackademic, codewithmukesh.com — eight common mistakes including anemic domain, over-mapping, wrong-layer repositories
- **ASP.NET Core middleware ordering:** ByteCrate.dev — canonical middleware order with pitfalls documented
- **EF Core pitfalls:** C# Corner — AsNoTracking, N+1, Include chains, DbContext lifetime
- **Multi-tenant isolation:** Agnite Studio — cross-tenant data leak guide, tenant isolation checklist; .NET Guide — EF Core global query filters, bypass patterns
- **Pet adoption marketplace failures:** LOW/CODE blog — "built for adopters without building for shelters first"; Social Animal — 200 shelter website audit (42.5% stale listings, 340% higher adoption inquiry with real-time sync); WeRescue — adoption gap as data infrastructure problem
- **1M user .NET scaling post-mortem:** Kerim Kara (Medium — Real World .NET) — pagination, caching, background jobs, external dependencies
- **Scalability / performance:** UnicornBurn startup autopsy methodology
- **VetCare (pet health multi-tenant reference):** paulo-raoni/vetcare on GitHub — multi-tenancy via EF Core global query filters, ADR-003

---

*Pitfalls research for: Pet Platform Web Application*
*Researched: 2026-07-18*
