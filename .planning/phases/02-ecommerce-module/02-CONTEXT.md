# Phase 2: E-Commerce Module - Context

**Gathered:** 2026-07-19
**Status:** Ready for planning

<domain>
## Phase Boundary

This phase delivers a full e-commerce module for pet products: product catalog with search/filter, shopping cart with per-user persistence, checkout with Stripe payment integration, order management with status tracking, and admin tools for product/category/inventory management.

</domain>

<decisions>
## Implementation Decisions

### Product Variants
- **D-01:** Products support variants with Size, Color, and Weight attributes
- **D-02:** Variant pricing uses base price + multiplier (product has base price, variants apply multiplier)
- **D-03:** Product-level image only — variants do not have separate images
- **D-04:** Inventory tracked per variant (each variant has its own stock count)
- **D-05:** SKUs are admin-assigned (not auto-generated)

### Payment Provider
- **D-06:** Stripe is the payment provider (using Stripe.net SDK)
- **D-07:** Stripe Elements (embedded) for checkout — card input form on the checkout page, not hosted redirect
- **D-08:** Payment confirmation uses webhook + redirect flow — Stripe sends webhook to confirm, user redirected to order confirmation page
- **D-09:** Stripe test mode configuration included in appsettings.Development.json from the start

### Cart Persistence
- **D-10:** Cart stored in database, linked to user account (per-user persistence)
- **D-11:** Out-of-stock items are blocked from being added to cart (strict enforcement)
- **D-12:** Cart quantity capped at available stock level (can't exceed inventory)
- **D-13:** Price locked at time of adding to cart (user sees the price they agreed to)
- **D-14:** Cart auto-empties after successful checkout
- **D-15:** Empty cart shows message with link to browse products

### Order Status Flow
- **D-16:** 4-status flow: Pending → Processing → Shipped → Delivered
- **D-17:** Only admins can change order statuses (users see status but cannot modify)
- **D-18:** Order enters "Pending" status immediately after successful payment
- **D-19:** Users see order status as badge on order list + detailed timeline on order detail page

### the agent's Discretion
- Category hierarchy structure (flat vs nested) — agent decides based on domain needs
- Product search implementation (full-text vs filtering) — agent decides based on SQL Server capabilities
- Admin dashboard UI layout for product management — agent decides based on existing admin patterns
- Shipping cost calculation approach — agent decides (flat rate, free shipping threshold, etc.)

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Project Architecture
- `.planning/PROJECT.md` — Clean Architecture constraints, tech stack decisions, entity patterns
- `.planning/REQUIREMENTS.md` — ECOM-01 through ECOM-08 requirements

### Phase 1 Patterns (must follow)
- `src/PetPlatform.Domain/Entities/Pet.cs` — Entity pattern: private setters, static Create factory, Guard.Against validation
- `src/PetPlatform.Application/Common/Result.cs` — Result<T> pattern for service responses
- `src/PetPlatform.Application/Interfaces/IApplicationDbContext.cs` — DbContext abstraction pattern
- `src/PetPlatform.Application/Services/PetService.cs` — Service layer pattern

### External Integrations
- Stripe.net SDK documentation — Payment integration reference
- Stripe Elements documentation — Embedded card input form reference

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `Result<T>` class: Use for all service method return types (ProductService, CartService, OrderService)
- `IApplicationDbContext`: Extend with new DbSet properties for Product, ProductVariant, Cart, Order entities
- `Guard.Against`: Use for domain entity validation in Create/Update methods

### Established Patterns
- Entity pattern: Private setters, static Create factory methods, private parameterless constructor for EF Core
- Service pattern: Interface in Application layer, implementation in Application/Services
- Validator pattern: FluentValidation validators in Application/Validators

### Integration Points
- `IApplicationDbContext` needs new DbSet properties for e-commerce entities
- Admin dashboard in Host.MVC needs new controllers for Product, Category, Inventory, Order management
- Customer-facing pages need new controllers for Catalog, Cart, Checkout, Order History

</code_context>

<specifics>
## Specific Ideas

No specific requirements — open to standard approaches

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 2-E-Commerce Module*
*Context gathered: 2026-07-19*
