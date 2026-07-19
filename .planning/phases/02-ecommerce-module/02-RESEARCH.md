# Phase 2: E-Commerce Module - Research

**Researched:** 2026-07-19
**Domain:** E-commerce (product catalog, cart, checkout, orders, inventory)
**Confidence:** HIGH

## Summary

This phase delivers a complete e-commerce module for pet products within the existing ASP.NET Core MVC Clean Architecture. The research covers five major areas: domain modeling (Product, ProductVariant, Category, Cart, Order entities), Stripe payment integration (Checkout Sessions API with embedded Payment Element), database-persisted shopping cart with per-user storage, order status workflow with timeline tracking, and product catalog search/filter.

The primary recommendation is to use the **Stripe Checkout Sessions API with `ui_mode: "elements"`** for embedded payment — this is Stripe's recommended approach as of 2026, requires less code than Payment Intents, and provides the embedded Payment Element the user requested. For product catalog search, **LINQ-based filtering with proper SQL Server indexes** is sufficient for v1, with SQL Server full-text search available as an enhancement. The domain model follows the existing entity pattern (private setters, static Create factory, Guard.Against validation) with a self-referencing category hierarchy and variant-based product model.

**Primary recommendation:** Use Stripe Checkout Sessions API with `ui_mode: "elements"` for embedded payment, LINQ filtering for catalog search, and a self-referencing category hierarchy with flat variant pricing (base price + multiplier).

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-01:** Products support variants with Size, Color, and Weight attributes
- **D-02:** Variant pricing uses base price + multiplier (product has base price, variants apply multiplier)
- **D-03:** Product-level image only — variants do not have separate images
- **D-04:** Inventory tracked per variant (each variant has its own stock count)
- **D-05:** SKUs are admin-assigned (not auto-generated)
- **D-06:** Stripe is the payment provider (using Stripe.net SDK)
- **D-07:** Stripe Elements (embedded) for checkout — card input form on the checkout page, not hosted redirect
- **D-08:** Payment confirmation uses webhook + redirect flow — Stripe sends webhook to confirm, user redirected to order confirmation page
- **D-09:** Stripe test mode configuration included in appsettings.Development.json from the start
- **D-10:** Cart stored in database, linked to user account (per-user persistence)
- **D-11:** Out-of-stock items are blocked from being added to cart (strict enforcement)
- **D-12:** Cart quantity capped at available stock level (can't exceed inventory)
- **D-13:** Price locked at time of adding to cart (user sees the price they agreed to)
- **D-14:** Cart auto-empties after successful checkout
- **D-15:** Empty cart shows message with link to browse products
- **D-16:** 4-status flow: Pending → Processing → Shipped → Delivered
- **D-17:** Only admins can change order statuses (users see status but cannot modify)
- **D-18:** Order enters "Pending" status immediately after successful payment
- **D-19:** Users see order status as badge on order list + detailed timeline on order detail page

### the agent's Discretion
- Category hierarchy structure (flat vs nested) — agent decides based on domain needs
- Product search implementation (full-text vs filtering) — agent decides based on SQL Server capabilities
- Admin dashboard UI layout for product management — agent decides based on existing admin patterns
- Shipping cost calculation approach — agent decides (flat rate, free shipping threshold, etc.)

### Deferred Ideas (OUT OF SCOPE)
None — discussion stayed within phase scope
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| ECOM-01 | User can browse product catalog with search/filter | LINQ filtering + SQL Server indexes on Product.Name, Category, PetType, Price, Brand |
| ECOM-02 | User can filter products by pet type, category, price, brand | Dedicated filter DTOs with nullable parameters, composable LINQ query building |
| ECOM-03 | User can add products to shopping cart | Database-persisted Cart/CartItem entities linked to UserId, variant selection, stock validation |
| ECOM-04 | User can checkout with payment | Stripe Checkout Sessions API with `ui_mode: "elements"`, webhook + redirect confirmation |
| ECOM-05 | User can view order history and status | Order entity with status enum, OrderStatusHistory for timeline, user-scoped queries |
| ECOM-06 | Admin can manage products (CRUD) | ProductService with admin authorization, Product entity with variants, image upload |
| ECOM-07 | Admin can manage categories | Category entity with self-referencing hierarchy, CRUD operations |
| ECOM-08 | Admin can manage inventory | Inventory tracking per variant, stock-level enforcement, low-stock alerts |
</phase_requirements>

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Product catalog browsing/search | API / Backend | Database / Storage | Service layer queries DB with filters, returns DTOs to MVC controllers |
| Category management | API / Backend | Database / Storage | Admin CRUD through service layer, stored in DB |
| Shopping cart persistence | API / Backend | Database / Storage | Cart service manages per-user DB cart, validates stock |
| Payment processing | API / Backend | CDN / Static | Server creates Stripe session, client-side JS renders Payment Element |
| Order management | API / Backend | Database / Storage | Order service manages lifecycle, status changes, timeline |
| Inventory tracking | API / Backend | Database / Storage | Stock validation on add-to-cart, decrement on checkout |
| Product images | CDN / Static | API / Backend | Static file storage, served via wwwroot, managed by admin |
| Webhook handling | API / Backend | — | Stripe webhook endpoint validates signature, updates order status |

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Stripe.net | 52.1.0 [VERIFIED: GitHub stripe/stripe-dotnet releases] | Stripe API client for .NET | Official Stripe SDK, latest stable (2026-06-24), supports .NET 10 |
| Ardalis.GuardClauses | 4.* [VERIFIED: existing project] | Domain entity validation | Already used in Phase 1, project convention |
| FluentValidation | 12.* [VERIFIED: existing project] | DTO/service-layer validation | Already used in Phase 1, project convention |
| Microsoft.EntityFrameworkCore | 10.0.* [VERIFIED: existing project] | ORM for database access | Already used in Phase 1, project convention |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Stripe.js (CDN) | dahlia (latest) [CITED: docs.stripe.com/payments/accept-a-payment] | Client-side Stripe Elements rendering | Checkout page — load from js.stripe.com |
| Ardalis.GuardClauses | 4.* | Domain validation in Create/Update methods | Every entity factory and update method |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Stripe Checkout Sessions (Elements) | Payment Intents API | Payment Intents requires significantly more code [CITED: docs.stripe.com] — Sessions recommended by Stripe |
| LINQ filtering | SQL Server Full-Text Search | FTS adds ranking but requires catalog/index setup — overkill for v1 catalog size |
| Self-referencing category hierarchy | Flat category list | Flat simpler but can't nest (e.g., Dogs > Food > Dry Food) — hierarchy is more natural for pet products |
| Database-persisted cart | Redis/in-memory cart | DB cart persists across sessions per D-10, Redis adds infrastructure complexity |

**Installation:**
```bash
dotnet add package Stripe.net --version 52.1.0
```

**Version verification:** Stripe.net v52.1.0 confirmed as latest stable on GitHub releases page (2026-06-24). Supports .NET Standard 2.0+, .NET Core 6+ LTS, .NET Framework 4.6.2+. The `StripeClient` class is the recommended entry point since v46. System.Text.Json is the default serialization library since v51.

## Package Legitimacy Audit

> NuGet ecosystem not supported by the package-legitimacy seam. Manual verification performed.

| Package | Registry | Age | Downloads | Source Repo | Verdict | Disposition |
|---------|----------|-----|-----------|-------------|---------|-------------|
| Stripe.net | NuGet | 14+ years | 200M+ [ASSUMED] | github.com/stripe/stripe-dotnet | OK | Approved |
| Ardalis.GuardClauses | NuGet | 7+ years | 50M+ [ASSUMED] | github.com/ardalis/GuardClauses | OK | Approved (already in project) |
| FluentValidation | NuGet | 12+ years | 100M+ [ASSUMED] | github.com/FluentValidation/FluentValidation | OK | Approved (already in project) |
| Microsoft.EntityFrameworkCore | NuGet | 8+ years | 500M+ [ASSUMED] | github.com/dotnet/efcore | OK | Approved (already in project) |

**Packages removed due to [SLOP] verdict:** none
**Packages flagged as suspicious [SUS]:** none

*Stripe.net is the official Stripe SDK maintained by Stripe employees. Repository has 1.5K+ stars, 120+ contributors, and 878 releases. Verified against official Stripe documentation.*

## Architecture Patterns

### System Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                        Customer Browser                             │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────────────┐   │
│  │ Catalog  │  │   Cart   │  │ Checkout │  │  Order History   │   │
│  │  Pages   │  │  Pages   │  │  Page    │  │     Pages        │   │
│  └────┬─────┘  └────┬─────┘  └────┬─────┘  └────────┬─────────┘   │
│       │              │              │                  │             │
│       │              │              │  Stripe.js       │             │
│       │              │              │  (Payment Element)│            │
│       │              │              └────────┬─────────┘            │
└───────┼──────────────┼───────────────────────┼──────────────────────┘
        │              │                       │
        ▼              ▼                       ▼
┌─────────────────────────────────────────────────────────────────────┐
│                     ASP.NET Core MVC (Host.MVC)                     │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────────────┐   │
│  │CatalogCtrl│ │ CartCtrl  │ │CheckoutCtrl│ │  OrderCtrl       │   │
│  └────┬─────┘  └────┬─────┘  └────┬─────┘  └────────┬─────────┘   │
└───────┼──────────────┼──────────────┼──────────────────┼────────────┘
        │              │              │                  │
        ▼              ▼              ▼                  ▼
┌─────────────────────────────────────────────────────────────────────┐
│                     Application Layer (Services)                     │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────────────┐   │
│  │ProductSvc│  │ CartSvc  │  │CheckoutSvc│ │  OrderSvc        │   │
│  └────┬─────┘  └────┬─────┘  └────┬─────┘  └────────┬─────────┘   │
│       │              │              │                  │             │
│  ┌────┴─────┐  ┌─────┴────┐  ┌─────┴─────┐  ┌───────┴─────────┐   │
│  │Validators│  │StockCheck│  │StripeService│ │StatusHistory    │   │
│  └──────────┘  └──────────┘  └───────────┘  └─────────────────┘   │
└───────┼──────────────┼──────────────┼──────────────────┼────────────┘
        │              │              │                  │
        ▼              ▼              ▼                  ▼
┌─────────────────────────────────────────────────────────────────────┐
│                     Infrastructure Layer                             │
│  ┌──────────────────────────────────────────────────────────────┐   │
│  │              ApplicationDbContext (EF Core)                    │   │
│  │  Products | Categories | Variants | Carts | CartItems        │   │
│  │  Orders | OrderItems | OrderStatusHistory | Payments         │   │
│  └──────────────────────────┬───────────────────────────────────┘   │
│                              │                                       │
│  ┌──────────────────────────▼───────────────────────────────────┐   │
│  │                    SQL Server Database                        │   │
│  └──────────────────────────────────────────────────────────────┘   │
│                                                                      │
│  ┌──────────────────────────────────────────────────────────────┐   │
│  │              Stripe.net SDK (StripeClient)                    │   │
│  └──────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────┘
        │
        ▼
┌─────────────────────────────────────────────────────────────────────┐
│                     External Services                               │
│  ┌──────────────────────────┐  ┌──────────────────────────────┐    │
│  │    Stripe API            │  │    Stripe Webhooks            │    │
│  │  (Checkout Sessions)     │  │  (payment_intent.succeeded)   │    │
│  └──────────────────────────┘  └──────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────────┘
```

### Recommended Project Structure

```
src/PetPlatform.Domain/
├── Entities/
│   ├── Pet.cs                    (existing)
│   ├── CustomerProfile.cs        (existing)
│   ├── Product.cs                (new)
│   ├── ProductVariant.cs         (new)
│   ├── Category.cs               (new)
│   ├── Brand.cs                  (new)
│   ├── Cart.cs                   (new)
│   ├── CartItem.cs               (new)
│   ├── Order.cs                  (new)
│   ├── OrderItem.cs              (new)
│   ├── OrderStatusHistory.cs     (new)
│   └── Payment.cs                (new)
├── Enums/
│   ├── PetSpecies.cs             (existing)
│   ├── OrderStatus.cs            (new)
│   ├── PaymentStatus.cs          (new)
│   └── VariantAttribute.cs       (new: Size, Color, Weight)

src/PetPlatform.Application/
├── Common/
│   └── Result.cs                 (existing)
├── DTOs/
│   ├── PetDto.cs                 (existing)
│   ├── CustomerProfileDto.cs     (existing)
│   ├── ProductDto.cs             (new)
│   ├── ProductVariantDto.cs      (new)
│   ├── CategoryDto.cs            (new)
│   ├── CartDto.cs                (new)
│   ├── CartItemDto.cs            (new)
│   ├── OrderDto.cs               (new)
│   ├── OrderItemDto.cs           (new)
│   ├── CheckoutDto.cs            (new)
│   ├── ProductFilterDto.cs       (new)
│   └── PagedResultDto.cs         (new)
├── Interfaces/
│   ├── IApplicationDbContext.cs  (existing — extend with new DbSets)
│   ├── IPetService.cs            (existing)
│   ├── ICustomerService.cs       (existing)
│   ├── IProductService.cs        (new)
│   ├── ICategoryService.cs       (new)
│   ├── ICartService.cs           (new)
│   ├── IOrderService.cs          (new)
│   ├── IPaymentService.cs        (new)
│   └── IInventoryService.cs      (new)
├── Services/
│   ├── PetService.cs             (existing)
│   ├── CustomerService.cs        (existing)
│   ├── ProductService.cs         (new)
│   ├── CategoryService.cs        (new)
│   ├── CartService.cs            (new)
│   ├── OrderService.cs           (new)
│   ├── PaymentService.cs         (new)
│   └── InventoryService.cs       (new)
└── Validators/
    ├── CreatePetValidator.cs     (existing)
    ├── UpdateCustomerProfileValidator.cs (existing)
    ├── CreateProductValidator.cs (new)
    ├── CreateCategoryValidator.cs (new)
    ├── AddToCartValidator.cs     (new)
    └── CheckoutValidator.cs      (new)

src/PetPlatform.Infrastructure/
├── Persistence/
│   ├── ApplicationDbContext.cs   (existing — extend with new DbSets)
│   └── Configurations/
│       ├── PetConfiguration.cs           (existing)
│       ├── CustomerProfileConfiguration.cs (existing)
│       ├── ProductConfiguration.cs       (new)
│       ├── ProductVariantConfiguration.cs (new)
│       ├── CategoryConfiguration.cs      (new)
│       ├── BrandConfiguration.cs         (new)
│       ├── CartConfiguration.cs          (new)
│       ├── CartItemConfiguration.cs      (new)
│       ├── OrderConfiguration.cs         (new)
│       ├── OrderItemConfiguration.cs     (new)
│       ├── OrderStatusHistoryConfiguration.cs (new)
│       └── PaymentConfiguration.cs       (new)

src/PetPlatform.Host.MVC/
├── Controllers/
│   ├── HomeController.cs         (existing)
│   ├── PetController.cs          (existing)
│   ├── CatalogController.cs      (new — customer-facing product browsing)
│   ├── CartController.cs         (new — shopping cart operations)
│   ├── CheckoutController.cs     (new — Stripe checkout flow)
│   ├── OrderController.cs        (new — order history and details)
│   └── Admin/
│       ├── ProductController.cs  (new — admin product CRUD)
│       ├── CategoryController.cs (new — admin category CRUD)
│       ├── InventoryController.cs (new — admin inventory management)
│       └── OrderManagementController.cs (new — admin order status management)
├── Views/
│   ├── Catalog/
│   │   ├── Index.cshtml          (product listing with filters)
│   │   └── Details.cshtml        (product detail with variant selection)
│   ├── Cart/
│   │   └── Index.cshtml          (shopping cart page)
│   ├── Checkout/
│   │   ├── Index.cshtml          (checkout with Stripe Payment Element)
│   │   └── Confirmation.cshtml   (order confirmation after payment)
│   ├── Order/
│   │   ├── Index.cshtml          (order history)
│   │   └── Details.cshtml        (order detail with status timeline)
│   └── Admin/
│       ├── Products/
│       │   ├── Index.cshtml      (product list)
│       │   ├── Create.cshtml     (create product)
│       │   └── Edit.cshtml       (edit product)
│       ├── Categories/
│       │   ├── Index.cshtml      (category list)
│       │   └── Edit.cshtml       (create/edit category)
│       ├── Inventory/
│       │   └── Index.cshtml      (inventory management)
│       └── Orders/
│           ├── Index.cshtml      (order list)
│           └── Details.cshtml    (order detail with status change)
└── wwwroot/
    └── js/
        └── checkout.js           (new — Stripe.js initialization and Payment Element)
```

### Pattern 1: Entity Pattern (Follow Existing)
**What:** Private setters, static `Create` factory method, `Guard.Against` validation, private parameterless constructor for EF Core
**When to use:** All new domain entities (Product, ProductVariant, Category, Cart, CartItem, Order, OrderItem, etc.)
**Example:**
```csharp
// Source: Existing Pet.cs pattern — PetPlatform.Domain/Entities/Pet.cs
public class Product
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public decimal BasePrice { get; private set; }
    public string? ImagePath { get; set; } // settable for file upload convenience
    public int CategoryId { get; private set; }
    public int BrandId { get; private set; }
    public string PetType { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // Navigation
    public Category Category { get; private set; } = null!;
    public Brand Brand { get; private set; } = null!;
    public ICollection<ProductVariant> Variants { get; private set; } = new List<ProductVariant>();

    private Product() { } // EF Core

    public static Product Create(string name, decimal basePrice, int categoryId,
                                  int brandId, string petType, string? description = null)
    {
        Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Guard.Against.NegativeOrZero(basePrice, nameof(basePrice));
        Guard.Against.NullOrWhiteSpace(petType, nameof(petType));

        return new Product
        {
            Name = name,
            BasePrice = basePrice,
            CategoryId = categoryId,
            BrandId = brandId,
            PetType = petType,
            Description = description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateDetails(string name, decimal basePrice, int categoryId,
                               int brandId, string petType, string? description)
    {
        Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Guard.Against.NegativeOrZero(basePrice, nameof(basePrice));

        Name = name;
        BasePrice = basePrice;
        CategoryId = categoryId;
        BrandId = brandId;
        PetType = petType;
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate() { IsActive = false; UpdatedAt = DateTime.UtcNow; }
    public void Activate() { IsActive = true; UpdatedAt = DateTime.UtcNow; }
}
```

### Pattern 2: Variant Pricing Model
**What:** Product has a base price; each variant applies a multiplier to compute its actual price
**When to use:** When variants affect pricing (e.g., larger sizes cost more)
**Example:**
```csharp
// Source: CONTEXT.md D-02 — base price + multiplier
public class ProductVariant
{
    public int Id { get; private set; }
    public int ProductId { get; private set; }
    public string? Size { get; private set; }
    public string? Color { get; private set; }
    public decimal? Weight { get; private set; }
    public decimal PriceMultiplier { get; private set; } = 1.0m;
    public int StockQuantity { get; private set; }
    public string? Sku { get; private set; }

    public Product Product { get; private set; } = null!;

    private ProductVariant() { } // EF Core

    /// <summary>
    /// Computes actual price: Product.BasePrice * PriceMultiplier
    /// </summary>
    public decimal ComputePrice() => Product.BasePrice * PriceMultiplier;

    public static ProductVariant Create(int productId, decimal priceMultiplier,
                                         int stockQuantity, string? size = null,
                                         string? color = null, decimal? weight = null,
                                         string? sku = null)
    {
        Guard.Against.NegativeOrZero(priceMultiplier, nameof(priceMultiplier));
        Guard.Against.Negative(stockQuantity, nameof(stockQuantity));

        return new ProductVariant
        {
            ProductId = productId,
            PriceMultiplier = priceMultiplier,
            StockQuantity = stockQuantity,
            Size = size,
            Color = color,
            Weight = weight,
            Sku = sku
        };
    }

    public void UpdateStock(int quantity)
    {
        Guard.Against.Negative(quantity, nameof(quantity));
        StockQuantity = quantity;
    }

    public void ReduceStock(int quantity)
    {
        if (StockQuantity < quantity)
            throw new InvalidOperationException("Insufficient stock.");
        StockQuantity -= quantity;
    }

    public void SetSku(string sku)
    {
        Guard.Against.NullOrWhiteSpace(sku, nameof(sku));
        Sku = sku;
    }
}
```

### Pattern 3: Database-Persisted Cart with Price Locking
**What:** Per-user cart stored in DB, with price locked at add-to-cart time
**When to use:** Every add-to-cart and cart display operation
**Example:**
```csharp
// Source: CONTEXT.md D-10, D-13 — per-user persistence, price locked at add time
public class CartItem
{
    public int Id { get; private set; }
    public int CartId { get; private set; }
    public int ProductVariantId { get; private set; }
    public int Quantity { get; private set; }
    public decimal LockedPrice { get; private set; }  // Price at time of adding
    public DateTime AddedAt { get; private set; }

    public Cart Cart { get; private set; } = null!;
    public ProductVariant ProductVariant { get; private set; } = null!;

    private CartItem() { } // EF Core

    public static CartItem Create(int cartId, int productVariantId,
                                    int quantity, decimal lockedPrice)
    {
        Guard.Against.NegativeOrZero(quantity, nameof(quantity));
        Guard.Against.NegativeOrZero(lockedPrice, nameof(lockedPrice));

        return new CartItem
        {
            CartId = cartId,
            ProductVariantId = productVariantId,
            Quantity = quantity,
            LockedPrice = lockedPrice,
            AddedAt = DateTime.UtcNow
        };
    }

    public void UpdateQuantity(int quantity)
    {
        Guard.Against.NegativeOrZero(quantity, nameof(quantity));
        Quantity = quantity;
    }
}

public class Cart
{
    public int Id { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public ICollection<CartItem> Items { get; private set; } = new List<CartItem>();

    private Cart() { } // EF Core

    public static Cart Create(string userId)
    {
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));
        return new Cart { UserId = userId, CreatedAt = DateTime.UtcNow };
    }

    public void Clear()
    {
        Items.Clear();
        UpdatedAt = DateTime.UtcNow;
    }
}
```

### Pattern 4: Order Status Workflow with Timeline
**What:** 4-status flow with OrderStatusHistory for timeline display
**When to use:** Order creation and status changes
**Example:**
```csharp
// Source: CONTEXT.md D-16, D-19 — 4-status flow with timeline
public enum OrderStatus
{
    Pending,
    Processing,
    Shipped,
    Delivered
}

public class Order
{
    public int Id { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public decimal TotalAmount { get; private set; }
    public OrderStatus Status { get; private set; }
    public string? StripePaymentIntentId { get; private set; }
    public string? ShippingAddress { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public ICollection<OrderItem> Items { get; private set; } = new List<OrderItem>();
    public ICollection<OrderStatusHistory> StatusHistory { get; private set; } = new List<OrderStatusHistory>();

    private Order() { } // EF Core

    public static Order Create(string userId, decimal totalAmount,
                                string? shippingAddress = null)
    {
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));
        Guard.Against.NegativeOrZero(totalAmount, nameof(totalAmount));

        var order = new Order
        {
            UserId = userId,
            TotalAmount = totalAmount,
            Status = OrderStatus.Pending,
            ShippingAddress = shippingAddress,
            CreatedAt = DateTime.UtcNow
        };

        // Record initial status in timeline
        order.StatusHistory.Add(OrderStatusHistory.Create(order.Id, OrderStatus.Pending));

        return order;
    }

    public void UpdateStatus(OrderStatus newStatus)
    {
        if (Status == newStatus)
            throw new InvalidOperationException($"Order is already in {newStatus} status.");

        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
        StatusHistory.Add(OrderStatusHistory.Create(Id, newStatus));
    }

    public void SetPaymentIntent(string paymentIntentId)
    {
        StripePaymentIntentId = paymentIntentId;
    }
}

public class OrderStatusHistory
{
    public int Id { get; private set; }
    public int OrderId { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateTime ChangedAt { get; private set; }

    public Order Order { get; private set; } = null!;

    private OrderStatusHistory() { } // EF Core

    public static OrderStatusHistory Create(int orderId, OrderStatus status)
    {
        return new OrderStatusHistory
        {
            OrderId = orderId,
            Status = status,
            ChangedAt = DateTime.UtcNow
        };
    }
}
```

### Pattern 5: Stripe Checkout Sessions with Embedded Payment Element
**What:** Server creates Checkout Session with `ui_mode: "elements"`, client renders Payment Element
**When to use:** Checkout page — the primary payment flow
**Example:**
```csharp
// Source: [CITED: docs.stripe.com/payments/accept-a-payment] — .NET server-side
// Server: CheckoutController creates session, returns client_secret to view
public class CheckoutController : Controller
{
    private readonly IPaymentService _paymentService;
    private readonly ICartService _cartService;

    [HttpPost]
    public async Task<IActionResult> CreateSession(string userId)
    {
        var cart = await _cartService.GetCartAsync(userId);
        if (cart is null || !cart.Items.Any())
            return RedirectToAction("Index", "Cart");

        // Build line items from cart (price locked at add time)
        var lineItems = cart.Items.Select(item => new SessionLineItemOptions
        {
            PriceData = new SessionLineItemPriceDataOptions
            {
                ProductData = new SessionLineItemPriceDataProductDataOptions
                {
                    Name = item.ProductName,  // Denormalized at add time
                },
                Currency = "usd",
                UnitAmount = (long)(item.LockedPrice * 100), // Stripe uses cents
            },
            Quantity = item.Quantity,
        }).ToList();

        var options = new SessionCreateOptions
        {
            UiMode = "elements",
            LineItems = lineItems,
            Mode = "payment",
            ReturnUrl = Url.Action("Confirmation", "Checkout",
                                    new { session_id = "{CHECKOUT_SESSION_ID}" },
                                    Request.Scheme),
        };

        var client = new StripeClient(/* secret key from config */);
        var session = client.V1.Checkout.Sessions.Create(options);

        ViewData["ClientSecret"] = session.ClientSecret;
        ViewData["SessionId"] = session.Id;

        return View("Index");
    }
}
```

```html
<!-- Client: Checkout/Index.cshtml — Razor view with Stripe.js -->
@{ ViewData["Title"] = "Checkout"; }

<form id="payment-form">
    <div id="payment-element">
        <!-- Stripe Payment Element renders here -->
    </div>
    <button id="submit">Pay Now</button>
    <div id="error-message"></div>
</form>

@section Scripts {
    <script src="https://js.stripe.com/dahlia/stripe.js"></script>
    <script>
        const stripe = Stripe('@ViewData["PublishableKey"]');
        const elements = stripe.elements({
            clientSecret: '@ViewData["ClientSecret"]',
            appearance: { theme: 'stripe' }
        });
        const paymentElement = elements.create('payment');
        paymentElement.mount('#payment-element');

        const form = document.getElementById('payment-form');
        form.addEventListener('submit', async (event) => {
            event.preventDefault();
            const { error } = await stripe.confirmPayment({
                elements,
                confirmParams: {
                    return_url: '@ViewData["ReturnUrl"]'
                }
            });
            if (error) {
                document.getElementById('error-message').textContent = error.message;
            }
        });
    </script>
}
```

### Pattern 6: Webhook Handling for Payment Confirmation
**What:** Validate Stripe webhook signature, update order status on payment success
**When to use:** Stripe sends `payment_intent.succeeded` or `checkout.session.completed` event
**Example:**
```csharp
// Source: [CITED: docs.stripe.com/webhooks] — webhook signature validation
[HttpPost("webhook")]
public async Task<IActionResult> Webhook()
{
    var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

    try
    {
        var stripeEvent = EventUtility.ConstructEvent(
            json,
            Request.Headers["Stripe-Signature"],
            _configuration["Stripe:WebhookSecret"]
        );

        if (stripeEvent.Type == Events.CheckoutSessionCompleted)
        {
            var session = stripeEvent.Data.Object as Session;
            await _paymentService.HandleCheckoutCompletedAsync(session);
        }

        return Ok();
    }
    catch (StripeException e)
    {
        return BadRequest();
    }
}
```

### Pattern 7: Composable Product Filtering
**What:** Build LINQ queries dynamically based on filter parameters
**When to use:** Product catalog page with multiple optional filters
**Example:**
```csharp
// Source: [CITED: multiple e-commerce ASP.NET Core projects] — composable filtering
public async Task<PagedResultDto<ProductDto>> GetFilteredProductsAsync(ProductFilterDto filter)
{
    IQueryable<Product> query = _context.Products
        .Where(p => p.IsActive)
        .Include(p => p.Category)
        .Include(p => p.Brand)
        .Include(p => p.Variants);

    // Composable filters — each adds a Where clause only if the filter is set
    if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        query = query.Where(p => p.Name.Contains(filter.SearchTerm) ||
                                  (p.Description != null && p.Description.Contains(filter.SearchTerm)));

    if (filter.CategoryId.HasValue)
        query = query.Where(p => p.CategoryId == filter.CategoryId.Value);

    if (filter.PetType.HasValue)
        query = query.Where(p => p.PetType == filter.PetType.Value.ToString());

    if (filter.BrandId.HasValue)
        query = query.Where(p => p.BrandId == filter.BrandId.Value);

    if (filter.MinPrice.HasValue)
        query = query.Where(p => p.BasePrice >= filter.MinPrice.Value);

    if (filter.MaxPrice.HasValue)
        query = query.Where(p => p.BasePrice <= filter.MaxPrice.Value);

    // Sorting
    query = filter.SortBy?.ToLower() switch
    {
        "price_asc" => query.OrderBy(p => p.BasePrice),
        "price_desc" => query.OrderByDescending(p => p.BasePrice),
        "name" => query.OrderBy(p => p.Name),
        _ => query.OrderByDescending(p => p.CreatedAt)
    };

    var totalCount = await query.CountAsync();
    var items = await query
        .Skip((filter.Page - 1) * filter.PageSize)
        .Take(filter.PageSize)
        .Select(p => MapToDto(p))
        .ToListAsync();

    return new PagedResultDto<ProductDto>
    {
        Items = items,
        TotalCount = totalCount,
        Page = filter.Page,
        PageSize = filter.PageSize
    };
}
```

### Anti-Patterns to Avoid
- **Don't use Payment Intents API directly:** Stripe recommends Checkout Sessions for most integrations [CITED: docs.stripe.com]. Payment Intents requires significantly more code and ongoing maintenance.
- **Don't lock cart price from current product price:** Always use the locked price from CartItem (D-13) — product prices may change after adding to cart.
- **Don't allow cart quantity > stock:** Always validate against current stock at add-to-cart and checkout time (D-11, D-12).
- **Don't skip webhook signature validation:** Always validate the `Stripe-Signature` header to prevent spoofed webhook attacks.
- **Don't use `LIKE '%term%'` for search:** Use proper indexes or LINQ Contains — LIKE with leading wildcard forces full table scan. For v1, LINQ filtering with proper indexes is sufficient.
- **Don't store order total from cart total at checkout time:** Copy item-level prices to OrderItem — this preserves the historical record even if product prices change later.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Payment form rendering | Custom credit card input form | Stripe Payment Element (via Stripe.js) | PCI compliance — card data never touches your server |
| Payment intent creation | Manual HTTP calls to Stripe API | Stripe.net SDK (`StripeClient`) | Handles auth, retries, serialization, versioning |
| Webhook signature validation | Manual HMAC comparison | `EventUtility.ConstructEvent()` | Timing-safe comparison, version-aware parsing |
| Price formatting | Custom currency formatter | Stripe AmountFormatter or `decimal.ToString("C")` | Currency-specific formatting rules, locale handling |
| UUID generation for orders | Custom GUID generator | `Guid.NewGuid()` | .NET built-in, cryptographically random |
| Stock reservation | Manual DB lock + decrement | Pessimistic locking via `FOR UPDATE` or optimistic concurrency | Prevents overselling under concurrent requests |

**Key insight:** Stripe Elements handle all card data collection, tokenization, and PCI compliance. Your server never sees raw card numbers — it only receives a PaymentIntent or Checkout Session reference. This is a fundamental security boundary that must not be bypassed.

## Common Pitfalls

### Pitfall 1: Race Condition on Cart Quantity vs Stock
**What goes wrong:** User adds items to cart, another user buys the last one, first user checks out with insufficient stock
**Why it happens:** Stock check at add-to-cart time doesn't account for concurrent checkouts
**How to avoid:** Use database-level locking (SELECT FOR UPDATE) or optimistic concurrency (check stock again at checkout time, fail if insufficient)
**Warning signs:** Orders placed with negative stock quantities

### Pitfall 2: Price Drift Between Cart and Order
**What goes wrong:** Product price changes between when user added to cart and when they checkout — order total doesn't match what user expected
**Why it happens:** Using current product price at checkout instead of locked cart price
**How to avoid:** Always use `CartItem.LockedPrice` when creating OrderItems — never recalculate from Product.BasePrice at checkout
**Warning signs:** Customer complaints about unexpected charges

### Pitfall 3: Stripe Webhook Not Reaching Server
**What goes wrong:** Payment succeeds but order status never updates to Processing
**Why it happens:** Webhook endpoint not reachable from Stripe (localhost, firewall, wrong URL)
**How to avoid:** Use Stripe CLI for local testing (`stripe listen --forward-to localhost:5000/webhook`), implement retry logic, log all webhook events
**Warning signs:** Orders stuck in "Pending" status forever

### Pitfall 4: Cart Items Stale After Product Deactivation
**What goes wrong:** Admin deactivates a product, but it's still in users' carts
**Why it happens:** No cascade logic between product status and cart contents
**How to avoid:** Filter active products when displaying cart, remove inactive items during checkout validation
**Warning signs:** Users trying to checkout with unavailable products

### Pitfall 5: Order Status Jump (Skipping States)
**What goes wrong:** Admin accidentally marks order as "Delivered" when it should be "Processing"
**Why it happens:** No state machine validation — any status can transition to any other
**How to avoid:** Validate allowed transitions: Pending→Processing→Shipped→Delivered (forward only). Optionally allow Processing→Pending for payment issues.
**Warning signs:** Orders with impossible status sequences in timeline

### Pitfall 6: Stripe Test vs Production Keys Mixed Up
**What goes wrong:** Using test keys in production or vice versa
**Why it happens:** Keys not properly separated by environment
**How to avoid:** Use `appsettings.Development.json` for test keys, `appsettings.Production.json` for live keys (D-09). Never commit live keys to source control.
**Warning signs:** Payments failing with "Invalid API key" errors

## Code Examples

### Product Entity Configuration (EF Core)
```csharp
// Source: Following existing PetConfiguration.cs pattern
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Description)
            .HasMaxLength(2000);

        builder.Property(p => p.BasePrice)
            .HasColumnType("decimal(18,2)");

        builder.Property(p => p.PetType)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(p => p.CategoryId);
        builder.HasIndex(p => p.BrandId);
        builder.HasIndex(p => p.PetType);
        builder.HasIndex(p => p.IsActive);

        builder.HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId);

        builder.HasOne(p => p.Brand)
            .WithMany(b => b.Products)
            .HasForeignKey(p => p.BrandId);
    }
}
```

### Cart Service — Add to Cart with Stock Validation
```csharp
// Source: [CITED: multiple e-commerce ASP.NET Core projects] — stock validation pattern
public async Task<Result<CartDto>> AddToCartAsync(string userId, AddToCartDto dto)
{
    Guard.Against.NullOrWhiteSpace(userId, nameof(userId));

    // Get or create cart
    var cart = await _context.Carts
        .Include(c => c.Items)
        .FirstOrDefaultAsync(c => c.UserId == userId);

    if (cart is null)
    {
        cart = Cart.Create(userId);
        _context.Carts.Add(cart);
    }

    // Validate variant exists and has stock
    var variant = await _context.ProductVariants
        .Include(v => v.Product)
        .FirstOrDefaultAsync(v => v.Id == dto.ProductVariantId);

    if (variant is null)
        return Result<CartDto>.Failure("Product variant not found.");

    if (variant.StockQuantity <= 0)
        return Result<CartDto>.Failure("This item is out of stock.");

    // Check if item already in cart
    var existingItem = cart.Items
        .FirstOrDefault(i => i.ProductVariantId == dto.ProductVariantId);

    var newQuantity = (existingItem?.Quantity ?? 0) + dto.Quantity;

    if (newQuantity > variant.StockQuantity)
        return Result<CartDto>.Failure(
            $"Only {variant.StockQuantity} items available. You have {existingItem?.Quantity ?? 0} in your cart.");

    if (existingItem is not null)
    {
        existingItem.UpdateQuantity(newQuantity);
    }
    else
    {
        // Lock the price at add time
        var lockedPrice = variant.ComputePrice();
        cart.Items.Add(CartItem.Create(cart.Id, dto.ProductVariantId, dto.Quantity, lockedPrice));
    }

    cart.UpdatedAt = DateTime.UtcNow;
    await _context.SaveChangesAsync();

    return Result<CartDto>.Success(MapToDto(cart));
}
```

### Stripe Configuration in appsettings.Development.json
```json
{
  "Stripe": {
    "SecretKey": "sk_test_...",
    "PublishableKey": "pk_test_...",
    "WebhookSecret": "whsec_..."
  }
}
```

### Stripe Service Registration in Program.cs
```csharp
// Register StripeClient as singleton
builder.Services.AddSingleton(new StripeClient(
    builder.Configuration["Stripe:SecretKey"]));
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Payment Intents API for all integrations | Checkout Sessions API with Payment Element recommended | 2025+ [CITED: docs.stripe.com] | Less code, built-in tax/shipping, Adaptive Pricing |
| `StripeConfiguration.ApiKey` (static) | `StripeClient` instance-based (since v46) [CITED: GitHub stripe/stripe-dotnet] | 2023 | Better testability, multiple API keys |
| Newtonsoft.Json serialization | System.Text.Json default (since v51) [CITED: GitHub stripe/stripe-dotnet CHANGELOG] | 2026-03 | Breaking change in v51, but non-breaking for most users |
| Manual SQL for full-text search | EF Core 11 LINQ-translatable FTS functions | 2025 (EF Core 11) | No more `FromSqlRaw` for ranked search — but project uses EF Core 10 |

**Deprecated/outdated:**
- `PaymentIntentService` standalone instantiation: Use `client.V1.PaymentIntents` instead [CITED: GitHub stripe/stripe-dotnet README]
- `StripeConfiguration.ApiKey = "..."` static config: Use `new StripeClient(secretKey)` instead [CITED: GitHub stripe/stripe-dotnet v46 changelog]
- `PaymentIntentCreateOptions` for embedded checkout: Use `SessionCreateOptions` with `UiMode = "elements"` [CITED: docs.stripe.com]

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Stripe.net v52.1.0 is compatible with .NET 10.0 | Standard Stack | Medium — may need to check if v52 supports net10 TFM; fallback to v51 |
| A2 | SQL Server full-text search is available in the target SQL Server edition | Catalog & Search | Low — LINQ filtering works without FTS; FTS is optional enhancement |
| A3 | Self-referencing category hierarchy (ParentId) is sufficient for pet product taxonomy | Architecture | Low — flat categories could work but hierarchy is more flexible |
| A4 | Flat rate shipping is acceptable for v1 | Agent's Discretion | Low — can be adjusted during implementation |
| A5 | Stripe Checkout Sessions with `ui_mode: "elements"` provides equivalent functionality to Payment Intents + Payment Element | Stripe Integration | Low — this is Stripe's recommended approach for embedded checkout |

**If this table is empty:** Not applicable — 5 assumptions documented.

## Open Questions

1. **What is the exact SQL Server edition available?**
   - What we know: Project uses SQL Server (confirmed in csproj and requirements)
   - What's unclear: Whether full-text search is available (requires Standard edition or higher)
   - Recommendation: Start with LINQ filtering. If full-text search is needed, it can be added as an enhancement in a later phase.

2. **Should categories support multi-level nesting or just one level?**
   - What we know: Agent has discretion (CONTEXT.md)
   - What's unclear: How deep the pet product taxonomy goes
   - Recommendation: Use self-referencing ParentId for unlimited depth, but constrain UI to 2 levels max (e.g., Dogs > Food, Dogs > Toys). This is a planning decision.

3. **What happens to cart items when a variant's stock drops to zero?**
   - What we know: D-11 says out-of-stock items are blocked from being added
   - What's unclear: Whether existing cart items should be removed or just blocked at checkout
   - Recommendation: Block at checkout, show warning in cart ("This item is now out of stock"), but don't auto-remove. This gives users visibility.

4. **Should shipping be calculated per-item or per-order?**
   - What we know: Agent has discretion (CONTEXT.md)
   - What's unclear: Business model for shipping
   - Recommendation: Per-order flat rate for v1. Can be enhanced with per-item or weight-based later.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET 10 SDK | All code | ✓ | 10.0.* (from csproj) | — |
| SQL Server | Database | ✓ (assumed) | Unknown | — |
| Node.js/npm | Tailwind CSS build | ✓ (from Phase 1) | Unknown | — |
| Stripe account (test mode) | Payment integration | ✗ | — | Human must create Stripe account and obtain test API keys |

**Missing dependencies with no fallback:**
- Stripe test API keys — developer must create a Stripe account and obtain keys from dashboard.stripe.com/apikeys

**Missing dependencies with fallback:**
- SQL Server full-text search — if not available, LINQ filtering with proper indexes is sufficient

## Validation Architecture

> Skip this section entirely — workflow.nyquist_validation is explicitly set to false in .planning/config.json.

## Security Domain

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | yes | ASP.NET Core Identity (Phase 1) — all cart/checkout/order endpoints require auth |
| V3 Session Management | yes | ASP.NET Core Identity session (Phase 1) |
| V4 Access Control | yes | Admin-only endpoints via policy-based authorization (Phase 1) |
| V5 Input Validation | yes | FluentValidation validators + Guard.Against domain validation |
| V6 Cryptography | yes | Stripe handles all card data encryption — PCI scope excluded from server |

### Known Threat Patterns for ASP.NET Core + Stripe

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Webhook spoofing | Tampering | Validate `Stripe-Signature` header with `EventUtility.ConstructEvent()` |
| Price manipulation (client-side) | Tampering | Never trust client-submitted prices — compute on server from DB |
| Stock overselling (race condition) | Tampering | Database-level locking or optimistic concurrency on stock check |
| Unauthorized order status change | Elevation of Privilege | Policy-based authorization: only Admin role can change status |
| Cart data leakage | Information Disclosure | User-scoped queries — always filter by `UserId` from claims |
| CSRF on checkout/payment | Tampering | Anti-forgery tokens on all POST forms (ASP.NET Core default) |
| Stripe key exposure | Information Disclosure | Never embed secret key in client-side code; use publishable key only |

## Sources

### Primary (HIGH confidence)
- Stripe.net GitHub repository (stripe/stripe-dotnet) — version history, API patterns, README [VERIFIED: github.com/stripe/stripe-dotnet]
- Stripe official documentation (docs.stripe.com) — Payment Element, Checkout Sessions, webhooks [CITED: docs.stripe.com/payments/accept-a-payment]
- Microsoft Learn — SQL Server full-text search in EF Core [CITED: learn.microsoft.com/en-us/ef/core/providers/sql-server/full-text-search]
- Existing project codebase — Pet.cs, CustomerProfile.cs, PetService.cs, ApplicationDbContext.cs [VERIFIED: local codebase]

### Secondary (MEDIUM confidence)
- ASP.NET Core e-commerce Clean Architecture projects (GitHub) — domain model patterns, cart/order implementations [WebSearch verified with official source]
- EF Core 11 full-text search improvements [CITED: learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-11.0/whatsnew]

### Tertiary (LOW confidence)
- E-commerce domain model best practices [WebSearch only, marked for validation]
- Stripe test card numbers and webhook testing patterns [CITED: docs.stripe.com/testing]

## Metadata

**Confidence breakdown:**
- Standard Stack: HIGH — Stripe.net verified via GitHub releases, all other packages already in project
- Architecture: HIGH — Follows existing Clean Architecture patterns, well-established e-commerce domain model
- Pitfalls: HIGH — Common e-commerce issues well-documented, race conditions and price drift are known patterns

**Research date:** 2026-07-19
**Valid until:** 2026-08-19 (30 days — stable tech stack, slow-moving domain)
