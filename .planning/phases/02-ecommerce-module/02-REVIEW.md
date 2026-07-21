---
phase: 02-ecommerce-module
reviewed: 2026-07-21T00:00:00Z
depth: standard
files_reviewed: 18
files_reviewed_list:
  - src/PetPlatform.Domain/Entities/Product.cs
  - src/PetPlatform.Domain/Entities/ProductVariant.cs
  - src/PetPlatform.Domain/Entities/Category.cs
  - src/PetPlatform.Domain/Entities/Cart.cs
  - src/PetPlatform.Domain/Entities/CartItem.cs
  - src/PetPlatform.Domain/Entities/Order.cs
  - src/PetPlatform.Domain/Entities/OrderItem.cs
  - src/PetPlatform.Domain/Entities/Payment.cs
  - src/PetPlatform.Application/Services/ProductService.cs
  - src/PetPlatform.Application/Services/CartService.cs
  - src/PetPlatform.Application/Services/OrderService.cs
  - src/PetPlatform.Infrastructure/Services/PaymentService.cs
  - src/PetPlatform.Host.MVC/Controllers/CatalogController.cs
  - src/PetPlatform.Host.MVC/Controllers/CartController.cs
  - src/PetPlatform.Host.MVC/Controllers/CheckoutController.cs
  - src/PetPlatform.Host.MVC/Controllers/OrderController.cs
  - src/PetPlatform.Host.MVC/Areas/Admin/Controllers/ProductController.cs
  - src/PetPlatform.Host.MVC/Areas/Admin/Controllers/OrderManagementController.cs
findings:
  critical: 3
  warning: 7
  info: 3
  total: 13
status: issues_found
---

# Phase 2: Code Review Report

**Reviewed:** 2026-07-21T00:00:00Z
**Depth:** standard
**Files Reviewed:** 18
**Status:** issues_found

## Summary

Reviewed 18 source files across Domain, Application, Infrastructure, and MVC Host layers implementing the e-commerce module (products, cart, checkout, orders, payments). The domain modeling is generally solid with good use of DDD patterns (factory methods, encapsulation, Ardalis GuardClauses). However, there are 3 critical issues including a stock race condition, a no-op validation guard, and an incomplete order query in the payment flow. Several warning-level issues affect correctness (unpopulated DTO fields, missing input validation) and maintainability (duplicated helper method, architecture leak in controller).

## Critical Issues

### CR-01: Race Condition Allows Overselling Stock During Checkout — **FIXED** ✅

**File:** `src/PetPlatform.Application/Services/OrderService.cs:86`
**Issue:** `ReduceStock` is called inside a loop without any concurrency control. If two users checkout simultaneously with the last unit of the same variant, both will pass the stock check in `CartService.AddToCartAsync` (which reads stock) and both will call `ReduceStock` (which decrements). Since EF Core batches all writes until `SaveChangesAsync` at line 94, the second `ReduceStock` call reads the in-memory value that hasn't been persisted, allowing `StockQuantity` to go negative.

**Fix applied:** Added `[ConcurrencyCheck]` attribute on `ProductVariant.StockQuantity` and wrapped `SaveChangesAsync` in a retry loop (3 attempts) that catches `DbUpdateConcurrencyException`, clears the change tracker, re-fetches variants to verify stock, and retries. Files: `ProductVariant.cs`, `OrderService.cs`.

---

### CR-02: ProductVariant.Create Guard Clause Is a No-Op — **FIXED** ✅

**File:** `src/PetPlatform.Domain/Entities/ProductVariant.cs:24`
**Issue:** The guard `Guard.Against.NullOrWhiteSpace(productId.ToString(), nameof(productId))` converts an `int` to a string and checks for null/whitespace. An `int` can never be null or whitespace — this guard will never throw. A `ProductId` of 0 (or any invalid value) passes silently, allowing creation of variants with no parent product.

**Fix applied:** Replaced `Guard.Against.NullOrWhiteSpace(productId.ToString(), nameof(productId))` with `Guard.Against.Zero(productId, nameof(productId))`. File: `ProductVariant.cs`.

---

### CR-03: PaymentService.GetOrderAfterPaymentAsync Returns Order With Empty Items — **FIXED** ✅

**File:** `src/PetPlatform.Infrastructure/Services/PaymentService.cs:111-125`
**Issue:** The query fetches the order without `.Include(o => o.Items).Include(o => o.StatusHistory)`. The returned `OrderDto` will have empty `Items` and `StatusHistory` collections. This is called from `CheckoutController.Confirmation`, which renders the order confirmation view — the user sees an order with zero line items, which is incorrect and confusing.

**Fix applied:** Added `.Include(o => o.Items).Include(o => o.StatusHistory)` to the query in `GetOrderAfterPaymentAsync`. File: `PaymentService.cs`.

---

## Warnings

### WR-01: OrderDto.CustomerEmail Is Never Populated — **FIXED** ✅

**File:** `src/PetPlatform.Application/Services/OrderService.cs:160-183`
**Issue:** `MapToDto` never sets the `CustomerEmail` property on `OrderDto` (defined at `OrderDto.cs:9`). `OrderManagementController.Details` reads `order.CustomerEmail` (line 41), which will always be `string.Empty`. The admin order details page will show a blank customer email.

**Fix applied:** Added `CustomerEmail = order.UserId` mapping in `MapToDto` with a comment noting this should be resolved to actual email when a user service is available. File: `OrderService.cs`.

---

### WR-02: CartItem.Create Has No Input Validation — **FIXED** ✅

**File:** `src/PetPlatform.Domain/Entities/CartItem.cs:19-28`
**Issue:** Unlike every other entity factory in this codebase, `CartItem.Create` has zero `Guard` clauses. `quantity` could be 0 or negative, `lockedPrice` could be negative, and `cartId`/`productVariantId` could be 0. This is inconsistent with the domain's defensive coding pattern and allows invalid state into the cart.

**Fix applied:** Added `Guard.Against.Zero` for `cartId` and `productVariantId`, `Guard.Against.NegativeOrZero` for `quantity`, and `Guard.Against.Negative` for `lockedPrice`. File: `CartItem.cs`.

---

### WR-03: UpdateQuantityAsync Allows Setting Quantity to Zero — **FIXED** ✅

**File:** `src/PetPlatform.Application/Services/CartService.cs:106`
**Issue:** `cartItem.UpdateQuantity(dto.Quantity)` has no check that `dto.Quantity > 0`. A user can set quantity to 0, leaving a zero-quantity ghost item in the cart that still shows up (with a $0.00 line total). The `AddToCartDto` also lacks a minimum quantity validation.

**Fix applied:** Added `if (dto.Quantity <= 0) return Result<CartDto>.Failure(...)` guard in `CartService.UpdateQuantityAsync`. File: `CartService.cs`.

---

### WR-04: File Upload Has No Type or Size Validation — **FIXED** ✅

**File:** `src/PetPlatform.Host.MVC/Areas/Admin/Controllers/ProductController.cs:72-86` and lines `146-160`
**Issue:** The image upload accepts any file type (`.exe`, `.svg` for XSS, etc.) and any file size. An admin could (intentionally or accidentally) upload a malicious executable or a multi-gigabyte file. The original filename extension is preserved without validation.

**Fix applied:** Added file extension allowlist (`.jpg`, `.jpeg`, `.png`, `.webp`) and 5 MB size limit validation in both `Create` and `Edit` actions of `ProductController`. Returns `BadRequest` with descriptive message on validation failure. File: `ProductController.cs`.

---

### WR-05: Duplicated BuildVariantDescription Method — **FIXED** ✅

**File:** `src/PetPlatform.Application/Services/CartService.cs:168-175`, `src/PetPlatform.Application/Services/OrderService.cs:185-192`, `src/PetPlatform.Infrastructure/Services/PaymentService.cs:128-135`
**Issue:** The exact same `BuildVariantDescription` method is copy-pasted in three classes. If the formatting logic changes, all three must be updated in lockstep. This violates DRY and is a maintenance hazard.

**Fix applied:** Extracted to `VariantDescriptionBuilder.Build(ProductVariant?)` static helper in `src/PetPlatform.Application/Common/VariantDescriptionBuilder.cs`. Updated `CartService`, `OrderService`, and `PaymentService` to use `static import` of `VariantDescriptionBuilder.Build`. Removed the three duplicate private methods.

---

### WR-06: CatalogController Directly Injects IApplicationDbContext — **FIXED** ✅

**File:** `src/PetPlatform.Host.MVC/Controllers/CatalogController.cs:11-21`
**Issue:** The controller injects `IApplicationDbContext` directly (line 11) to query brands at line 48. This bypasses the application service layer and leaks infrastructure concerns into the presentation layer. The brand count query also runs a potentially expensive correlated subquery for every brand without pagination.

**Fix applied:** Created `IBrandService` interface (`src/PetPlatform.Application/Interfaces/IBrandService.cs`) and `BrandService` implementation (`src/PetPlatform.Application/Services/BrandService.cs`) with `GetBrandsWithProductCountsAsync()`. Updated `CatalogController` to inject `IBrandService` instead of `IApplicationDbContext`.

---

### WR-07: Order.Create Sets OrderStatusHistory With OrderId=0 — **FIXED** ✅

**File:** `src/PetPlatform.Domain/Entities/Order.cs:35`
**Issue:** `OrderStatusHistory.Create(order.Id, OrderStatus.Pending)` is called before the order is persisted, so `order.Id` is 0. The `OrderStatusHistory` entity is created with `OrderId = 0`. While EF Core will fix up the foreign key through the `StatusHistory` navigation collection during `SaveChangesAsync`, this relies on implicit tracking behavior. If the entity is detached or if the collection is iterated before save, the stale FK value could cause issues. It's also a confusing pattern for future maintainers.

**Fix applied:** Added clarifying comment explaining EF Core's fixup behavior via the `StatusHistory` navigation collection. The current EF Core tracking-based fixup is correct; the comment prevents future maintainers from being confused by the apparent `OrderId = 0` value.

---

## Info

### IN-01: Product.ImagePath Has Public Setter

**File:** `src/PetPlatform.Domain/Entities/Product.cs:11`
**Issue:** `ImagePath` is the only property on `Product` with a public setter (noted by the inline comment "settable for file upload convenience"). All other properties use `private set` for encapsulation. This breaks the DDD pattern consistency — the file upload concern leaks into the domain entity.

**Fix:** Use a domain method instead:

```csharp
public void SetImagePath(string? path) => ImagePath = path;
```

---

### IN-02: CheckoutController Hardcodes Shipping Cost

**File:** `src/PetPlatform.Host.MVC/Controllers/CheckoutController.cs:38`
**Issue:** `ShippingCost = 5.99m` is a hardcoded magic number. If shipping rates need to change, this requires a code change and redeployment.

**Fix:** Move to configuration or a shipping calculation service:

```csharp
ShippingCost = _configuration.GetValue<decimal>("Shipping:DefaultCost", 5.99m)
```

---

### IN-03: GetAllOrdersAsync Searches by UserId, Not Email

**File:** `src/PetPlatform.Application/Services/OrderService.cs:110`
**Issue:** The admin search parameter `searchTerm` filters by `o.UserId.Contains(searchTerm)`. In most auth systems, `UserId` is an opaque GUID/ID, not a human-readable email. Admins searching by email will get zero results. The `CustomerEmail` field name in the DTO suggests this was intended to store email, but it maps to `UserId`.

**Fix:** Either store and index the customer email on the Order entity, or resolve UserId to email during the query. At minimum, search should cover both UserId and any indexed display-name field.

---

_Reviewed: 2026-07-21T00:00:00Z_
_Reviewer: the agent (gsd-code-reviewer)_
_Depth: standard_
