# Architecture Research

**Domain:** Pet platform web application (ASP.NET Core MVC + Clean Architecture)
**Researched:** 2026-07-18
**Confidence:** HIGH

## Standard Architecture

### System Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        HOST.MVC (Presentation Layer)                        │
│  Controllers · Razor Views · ViewModels · Middleware · wwwroot (JS/CSS)     │
│  Areas: Admin/ | Customer/ | Identity/                                     │
├─────────────────────────────────────────────────────────────────────────────┤
│                        APPLICATION (Business Logic Layer)                   │
│  Services · DTOs · Interfaces · Validators · Specifications · Mappings      │
│  Cross-cutting: Result pattern, Pagination, Claims-mapping                  │
├─────────────────────────────────────────────────────────────────────────────┤
│                        INFRASTRUCTURE (Adapters Layer)                      │
│  EF Core DbContext · Migrations · Repositories · Identity/Auth             │
│  External: Email (SMTP) · Image upload · Payment gateway stubs             │
├─────────────────────────────────────────────────────────────────────────────┤
│                        DOMAIN (Enterprise Rules Layer)                      │
│  Entities · Aggregates · Enums · Value Objects · Domain Events             │
│  Interfaces (abstractions consumed by upper layers)                         │
│  Zero external dependencies                                                 │
└─────────────────────────────────────────────────────────────────────────────┘

Data Flow Direction:
  Host.MVC → Application → Infrastructure → Domain
  (Dependency Inversion: Domain has zero deps; interfaces flow inward)
```

### Component Responsibilities

| Component | Responsibility | Typical Implementation |
|-----------|----------------|------------------------|
| **Domain Layer** | Core entities (Pet, Owner, Product, Order, MedicalRecord, AdoptionListing, LostPetReport), domain enums (OrderStatus, PetHealthStatus, AdoptionStatus), repository interfaces, domain events, value objects | Pure C# classes, no framework dependencies, unit-testable in isolation |
| **Application Layer** | Business logic services, use-case orchestration, DTOs for cross-layer communication, input validation, service interfaces | Service classes implementing domain interfaces, Result<T> pattern for error handling, FluentValidation validators |
| **Infrastructure Layer** | Data access (EF Core DbContext + repositories), external service adapters (email, file storage, payment), Identity configuration, EF migrations | Repository implementations, DbContext, migrations, concrete adapters for external concerns |
| **Host.MVC Layer** | HTTP request handling, view rendering, view model composition, middleware pipeline, DI composition root | MVC Controllers, Razor Views, ViewModels, Areas (Admin/Customer/Identity), Program.cs/Startup DI wiring |

## Recommended Project Structure

```
PetPlatform/
├── src/
│   ├── PetPlatform.Domain/              # Enterprise business rules
│   │   ├── Entities/                    # Core domain models
│   │   │   ├── Pet.cs                   # Pet (species, breed, age, medical flags)
│   │   │   ├── Owner.cs                 # Pet owner (linked to ApplicationUser)
│   │   │   ├── Product.cs               # E-commerce product
│   │   │   ├── Order.cs                 # Order header
│   │   │   ├── OrderItem.cs             # Order line item
│   │   │   ├── MedicalRecord.cs         # Vet visit / SOAP note
│   │   │   ├── Vaccination.cs           # Vaccination record
│   │   │   ├── AdoptionListing.cs       # Pet for adoption
│   │   │   ├── LostPetReport.cs         # Lost/found pet report
│   │   │   └── CartItem.cs              # Shopping cart item
│   │   ├── Enums/                       # Domain enumerations
│   │   │   ├── OrderStatus.cs           # Pending/Processing/Shipped/Delivered/Cancelled
│   │   │   ├── PetSpecies.cs            # Dog/Cat/Bird/Other
│   │   │   ├── AdoptionStatus.cs        # Available/Adopted/PendingReview
│   │   │   └── MedicalRecordType.cs     # Checkup/Vaccination/Surgery/Emergency
│   │   └── Interfaces/                  # Abstractions for infrastructure
│   │       ├── IRepository.cs           # Generic repository contract
│   │       ├── IUnitOfWork.cs           # Transaction management contract
│   │       ├── IEmailService.cs         # Email sending abstraction
│   │       └── IFileStorage.cs          # Image/file upload abstraction
│   │
│   ├── PetPlatform.Application/         # Application business rules
│   │   ├── Services/                    # Business logic implementations
│   │   │   ├── PetService.cs
│   │   │   ├── ProductService.cs
│   │   │   ├── OrderService.cs
│   │   │   ├── AdoptionService.cs
│   │   │   ├── LostPetService.cs
│   │   │   ├── MedicalRecordService.cs
│   │   │   └── CartService.cs
│   │   ├── DTOs/                        # Data transfer objects
│   │   │   ├── PetDto.cs
│   │   │   ├── ProductDto.cs
│   │   │   └── ...
│   │   ├── Interfaces/                  # Service contracts (for DI)
│   │   │   ├── IPetService.cs
│   │   │   ├── IProductService.cs
│   │   │   └── ...
│   │   └── Common/                      # Shared utilities
│   │       ├── Result.cs               # Result<T> pattern
│   │       └── Pagination.cs           # Paginated list helper
│   │
│   ├── PetPlatform.Infrastructure/      # External concerns
│   │   ├── Persistence/
│   │   │   ├── ApplicationDbContext.cs  # EF Core DbContext
│   │   │   ├── Configurations/          # Fluent API entity configs
│   │   │   └── Migrations/             # EF Core migrations
│   │   ├── Repositories/               # Repository implementations
│   │   │   ├── GenericRepository.cs
│   │   │   ├── ProductRepository.cs
│   │   │   └── ...
│   │   ├── Services/                    # Infrastructure services
│   │   │   ├── EmailService.cs
│   │   │   ├── FileStorageService.cs
│   │   │   └── IdentityService.cs
│   │   └── Identity/                    # ASP.NET Core Identity setup
│   │       └── SeedData.cs             # Role + permission seeding
│   │
│   └── PetPlatform.Host.MVC/            # Presentation layer
│       ├── Controllers/
│       │   ├── HomeController.cs
│       │   ├── PetController.cs
│       │   ├── ProductController.cs
│       │   ├── OrderController.cs
│       │   ├── CartController.cs
│       │   ├── AdoptionController.cs
│       │   ├── LostPetController.cs
│       │   └── Areas/
│       │       ├── Admin/
│       │       │   ├── Controllers/
│       │       │   │   ├── DashboardController.cs
│       │       │   │   ├── UserController.cs
│       │       │   │   ├── ProductController.cs
│       │       │   │   └── OrderController.cs
│       │       │   └── Views/
│       │       ├── Customer/
│       │       │   ├── Controllers/
│       │       │   │   ├── AccountController.cs
│       │       │   │   └── MyPetsController.cs
│       │       │   └── Views/
│       │       └── Identity/
│       │           └── Pages/
│       ├── ViewModels/                 # Presentation models
│       │   ├── ProductListViewModel.cs
│       │   ├── CartViewModel.cs
│       │   ├── AdminDashboardViewModel.cs
│       │   └── ...
│       ├── Views/                       # Razor views
│       ├── ViewComponents/             # Reusable UI components
│       ├── wwwroot/                     # Static assets (JS, CSS, images)
│       │   ├── css/                    # Tailwind CSS output
│       │   ├── js/                     # jQuery + custom scripts
│       │   └── lib/                    # Third-party libraries
│       ├── Program.cs                  # Composition root (DI wiring)
│       ├── appsettings.json
│       └── tailwind.config.js
│
├── tests/
│   ├── PetPlatform.Domain.Tests/
│   ├── PetPlatform.Application.Tests/
│   ├── PetPlatform.Infrastructure.Tests/
│   └── PetPlatform.Host.MVC.Tests/
│
└── PetPlatform.sln
```

### Structure Rationale

- **Domain/:** Pure business rules with zero framework dependencies. Entities and interfaces live here so both Application and Infrastructure can reference Domain without circular deps. Repository interfaces defined here, not in Application — this follows the Microsoft eShopOnWeb pattern where Domain owns the abstractions.
- **Application/:** Contains business logic services that orchestrate domain entities. Services depend on interfaces defined in Domain. DTOs keep domain entities out of the presentation layer. The Result<T> pattern provides type-safe error handling without exceptions for business rule violations.
- **Infrastructure/:** Concrete implementations of Domain interfaces. EF Core DbContext, repository implementations, external service adapters (email, file storage). This is where framework coupling lives — contained and replaceable.
- **Host.MVC/:** The entry point and composition root. MVC Controllers compose ViewModels from Application service results. Areas (Admin, Customer) separate concerns by user role. Program.cs wires DI — the only place that knows about all four layers.

## Architectural Patterns

### Pattern 1: Repository + Unit of Work

**What:** Abstracts data access behind interfaces defined in Domain, with Unit of Work coordinating transactions across multiple repositories.
**When to use:** Any project with EF Core that needs testable data access and transactional consistency across multiple entity operations.
**Trade-offs:** Adds boilerplate for simple CRUD, but pays dividends when business operations span multiple repositories (e.g., placing an order = create Order + update Product stock + create OrderItems).
**Reference:** KeystoneCommerce, VetCare, Microsoft eShopOnWeb all implement this pattern successfully.

```csharp
// Domain layer defines the contract
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task AddAsync(T entity);
    void Update(T entity);
    void Remove(T entity);
}

public interface IUnitOfWork : IDisposable
{
    IProductRepository Products { get; }
    IOrderRepository Orders { get; }
    IPetRepository Pets { get; }
    Task<int> SaveChangesAsync();
}

// Application layer uses the contract
public class OrderService : IOrderService
{
    private readonly IUnitOfWork _unitOfWork;
    public async Task<Result<OrderDto>> PlaceOrderAsync(CreateOrderDto dto)
    {
        // Business logic + multiple repo operations + single SaveChanges
    }
}
```

### Pattern 2: Result<T> Pattern for Error Handling

**What:** Services return Result<T> with success/failure states instead of throwing exceptions for business rule violations.
**When to use:** When business logic failures are expected and recoverable (validation errors, stock unavailable, duplicate email).
**Trade-offs:** More verbose than exceptions, but makes error paths explicit and testable. Avoids try-catch abuse for flow control.
**Reference:** KeystoneCommerce uses this extensively; VetCare uses MediatR + FluentValidation pipeline instead — both valid approaches.

```csharp
public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }
    
    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(string error) => new(false, default, error);
}
```

### Pattern 3: Area-Based MVC Organization

**What:** Uses ASP.NET Core Areas to partition controllers, views, and view models by user role or functional domain.
**When to use:** When the application serves distinct user roles (Admin, Customer, Vet) with different views and permissions for the same entities.
**Trade-offs:** Adds routing complexity (`/Admin/Product/Index` vs `/Product/Index`), but provides natural permission boundaries and keeps controller files focused.
**Reference:** Multiple ASP.NET Core MVC e-commerce projects use Area/Admin + Area/Customer + Area/Identity pattern.

```
Areas/
├── Admin/           # Admin dashboard, user management, product management
│   ├── Controllers/ # DashboardController, UserController, ProductController
│   └── Views/       # Admin-specific layouts and views
├── Customer/        # Customer account, order history, pet management
│   ├── Controllers/ # AccountController, MyPetsController
│   └── Views/       # Customer-specific layouts and views
└── Identity/        # Login, Register, Password Reset (scaffolded)
    └── Pages/       # Razor Pages for auth flows
```

### Pattern 4: Claims-Based Permission System (Layered on Identity)

**What:** ASP.NET Core Identity roles as the foundation, with Claims-based permissions layered on top for granular access control.
**When to use:** When you need fine-grained permissions (e.g., "can manage products" vs "can view orders") without creating a new role for every combination.
**Trade-offs:** More setup than simple role checks, but enables future permission additions without code changes — just add a new claim.
**Reference:** VetCare uses role-based policies (AdminOnly, VetOrAdmin, AnyStaff); this project extends with Claims for more flexibility.

```csharp
// Permission seeding in Infrastructure/Identity/SeedData.cs
await userManager.AddClaimAsync(admin, new Claim("Permissions", "Products.Manage"));
await userManager.AddClaimAsync(admin, new Claim("Permissions", "Users.Manage"));
await userManager.AddClaimAsync(vet, new Claim("Permissions", "MedicalRecords.Create"));

// Controller usage
[Authorize(Policy = "Permission:Products.Manage")]
public class ProductController : Controller { ... }
```

## Data Flow

### Request Flow (Typical MVC Request)

```
[User Action: Click "Add to Cart"]
    ↓
[Controller: CartController.AddItem()]
    ↓ (calls service via interface)
[Application: CartService.AddItemToCartAsync()]
    ↓ (business logic: validate product exists, check stock)
    ↓ (calls repository via IUnitOfWork)
[Infrastructure: CartItemRepository + SaveChangesAsync()]
    ↓ (EF Core maps to SQL)
[SQL Server: INSERT into CartItems]
    ↓
[Response: Redirect to Cart page with success message]
```

### State Management

```
┌─────────────────────────────────────────────────────────────────┐
│                     SERVER-SIDE STATE                           │
│                                                                 │
│  [Session/Cookie] ←→ [Cart]     (Shopping cart per user)        │
│  [DbContext Tracking] ←→ [Entities] (Per-request unit of work) │
│  [Identity Claims] ←→ [User Permissions] (Per authenticated req)│
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                     CLIENT-SIDE STATE                           │
│                                                                 │
│  [jQuery + AJAX] ←→ [DOM updates] (Cart preview, search filter)│
│  [Tailwind CSS] ←→ [Responsive layout] (Mobile/desktop)        │
│  [Form data] ←→ [Hidden fields / ViewModels] (Multi-step forms)│
└─────────────────────────────────────────────────────────────────┘
```

### Key Data Flows

1. **E-Commerce Purchase Flow:** Browse products → Add to cart (cookie or session) → Checkout (create Order + OrderItems) → Update inventory (decrement stock atomically via stored procedure or EF) → Payment processing → Order confirmation email
2. **Medical Records Flow:** Vet creates appointment → Conducts exam → Creates MedicalRecord (SOAP notes) → Records Vaccination → Uploads lab results → Owner views pet history
3. **Adoption Flow:** Shelter creates AdoptionListing (with pet details, photos) → Customer browses/searches → Customer submits adoption application → Admin/Shelter reviews → Approval/rejection with notification
4. **Lost Pet Flow:** Owner submits LostPetReport (photo, location, description) → Report appears on public lost pets page → Other users can submit tips/matches → Owner marks as found
5. **Admin Dashboard Flow:** Admin views aggregated metrics (orders, users, products) → Manages user roles/permissions → Reviews and approves adoption listings → Monitors inventory health

## Scaling Considerations

| Scale | Architecture Adjustments |
|-------|--------------------------|
| 0-1k users | Monolith is fine. SQL Server single instance. In-memory caching. No background job complexity needed. |
| 1k-100k users | Add Redis caching for product catalog and cart. Consider background job processing (Hangfire) for email, inventory expiration. CDN for static assets. |
| 100k+ users | Consider read replicas for SQL Server. Move to containerized deployment. Potential microservice extraction for payment processing and notifications. |

### Scaling Priorities

1. **First bottleneck:** Product catalog queries under high browse traffic → Add Redis caching with prefix-based invalidation
2. **Second bottleneck:** Order processing under flash sales → Add Hangfire background jobs for inventory reservation expiration, email queuing
3. **Third bottleneck:** Database write contention during checkout → Consider CQRS (read/write model separation) or stored procedures with row-level locking (as KeystoneCommerce demonstrates)

## Anti-Patterns

### Anti-Pattern 1: Domain Layer Depending on Infrastructure

**What people do:** Placing EF Core attributes or DbContext references in Domain entities.
**Why it's wrong:** Violates dependency inversion; Domain becomes untestable without a database; infrastructure changes ripple into business logic.
**Do this instead:** Use Fluent API configurations in Infrastructure/Persistence/Configurations/ to map entities. Keep Domain entities as plain C# classes.

### Anti-Pattern 2: Business Logic in Controllers

**What people do:** Writing validation, stock checks, and order rules directly in controller actions.
**Why it's wrong:** Controllers become untestable, logic can't be reused across areas (Admin vs Customer), violates Single Responsibility.
**Do this instead:** Controllers should only: parse input → call Application service → compose ViewModel from result → return View. All business logic lives in Application services.

### Anti-Pattern 3: Shared ViewModel Across Areas

**What people do:** Using the same ViewModel for Admin product edit and Customer product detail.
**Why it's wrong:** Admin needs fields like stock count, margin, supplier info. Customer needs reviews, related products. One fat ViewModel serves neither well.
**Do this instead:** Create separate ViewModels per Area: AdminProductEditViewModel, CustomerProductDetailViewModel. Duplicating a few properties is fine — the views diverge over time anyway.

### Anti-Pattern 4: Ignoring Transaction Boundaries in Checkout

**What people do:** Creating Order, updating stock, and sending confirmation email in separate, non-transactional operations.
**Why it's wrong:** Stock decrements but order fails → inventory ghost. Order created but email fails → user confused. Partial failures create data inconsistency.
**Do this instead:** Wrap Order creation + stock update in a single UnitOfWork.SaveChangesAsync. Send confirmation email as a background job after commit succeeds. Use the Result pattern to handle payment failures gracefully.

## Integration Points

### External Services

| Service | Integration Pattern | Notes |
|---------|---------------------|-------|
| **Email (SMTP)** | Interface in Domain (`IEmailService`), implementation in Infrastructure | Use background job for async sending; don't block request on SMTP |
| **Image/File Storage** | Interface in Domain (`IFileStorage`), implementation in Infrastructure | Start with local filesystem; swap to blob storage later via interface |
| **Payment Gateway** | Interface in Domain (`IPaymentService`), implementation in Infrastructure | Stripe or similar; start with Cash on Delivery to validate flow |
| **SQL Server** | EF Core DbContext in Infrastructure | Code-First migrations; stored procedures for inventory atomicity |

### Internal Boundaries

| Boundary | Communication | Notes |
|----------|---------------|-------|
| **Host.MVC → Application** | Service interfaces (DI) | Controllers never reference Infrastructure types directly |
| **Application → Domain** | Entity + interface references | Application services orchestrate domain logic |
| **Infrastructure → Domain** | Implements domain interfaces | Infrastructure is the only layer that knows about EF Core |
| **Admin Area ↔ Customer Area** | Shared Domain entities, separate ViewModels | Same data, different views and permissions |

## Module Dependency Map (Build Order Implications)

```
Phase 1 Foundation:
  Domain → Application → Infrastructure → Host.MVC
  (Auth/Identity, User Management, Admin Dashboard foundation)

Phase 2 E-Commerce (depends on Phase 1):
  Domain (Product, Cart, Order entities) → Application (ProductService, OrderService)
  → Infrastructure (repositories, payment) → Host.MVC (ProductController, CartController)

Phase 3 Pet Management (depends on Phase 1):
  Domain (Pet, Owner entities) → Application (PetService)
  → Infrastructure (repositories) → Host.MVC (PetController, MyPets)

Phase 4 Adoption + Lost Pets (depends on Phase 3):
  Domain (AdoptionListing, LostPetReport) → Application (AdoptionService, LostPetService)
  → Infrastructure (repositories, image storage) → Host.MVC (AdoptionController, LostPetController)

Phase 5 Medical Records (depends on Phase 3):
  Domain (MedicalRecord, Vaccination) → Application (MedicalRecordService)
  → Infrastructure (repositories) → Host.MVC (Vet dashboard, pet health history)
```

## Sources

- **Microsoft eShopOnWeb** (learn.microsoft.com/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures) — Official Clean Architecture reference for ASP.NET Core. Confirms 4-layer structure: Application Core (Domain), Infrastructure, UI Layer. HIGH confidence.
- **VetCare** (github.com/paulo-raoni/vetcare) — Multi-tenant veterinary SaaS on .NET 8 Clean Architecture. Directly relevant domain entities (Pet, Owner, Appointment, Vaccination). Demonstrates Domain → Application → Infrastructure → Api layer separation. HIGH confidence.
- **KeystoneCommerce** (github.com/Zyad-Eltayabi/KeystoneCommerce) — ASP.NET Core MVC e-commerce with Clean Architecture. Demonstrates Repository + Unit of Work, inventory management with stored procedures, cookie-based cart, admin dashboard with analytics. HIGH confidence.
- **Bhaw Bhaw Pet Marketplace** (xenotixlabs.com) — Case study of pet care marketplace platform. Modular architecture: commerce system, service booking, vendor management, customer engagement. HIGH confidence.
- **Zulbera Marketplace Guide** (zulbera.com) — Marketplace MVP components: user accounts, listings/search, messaging, payment, reviews, admin. Recommended build sequence. MEDIUM confidence.
- **DEV.to Pet Marketplace 2026** (dev.to/viktoriaholikova) — Pet-specific marketplace considerations: trust architecture, structured pet listings (breed/vaccination/age/microchip), compliance readiness. MEDIUM confidence.
- **Pawlly Pet Services SaaS** (blackbyrds.digital) — 31-module pet services marketplace. Demonstrates service booking, commission tracking, subscription plans, pet profile management. MEDIUM confidence.
- **Pet Marketplace Architecture** (lowcode.agency) — Five architectural layers: frontend, backend API, data, infrastructure, integration. Structured monolith recommended as default. MEDIUM confidence.

---
*Architecture research for: Pet Platform (ASP.NET Core MVC + Clean Architecture)*
*Researched: 2026-07-18*
