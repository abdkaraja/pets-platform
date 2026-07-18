# Stack Research

**Domain:** ASP.NET Core MVC Pet Platform (Clean Architecture)
**Researched:** 2026-07-18
**Confidence:** HIGH

## Recommended Stack

### Core Technologies

| Technology | Version | Purpose | Why Recommended |
|------------|---------|---------|-----------------|
| .NET 10 SDK | 10.0.301+ | Runtime and SDK | Latest LTS release (Nov 2025), supported until Nov 2028. C# 14, better JIT, improved perf. Alternatives: .NET 8 (LTS but nearing EOL Nov 2026), .NET 9 (STS, EOL Nov 2026). |
| ASP.NET Core MVC | 10.0 | Web framework | Ships with .NET 10. Server-rendered Razor Views, tag helpers, model binding — the project's stated constraint. |
| Entity Framework Core | 10.0.10 | ORM / Code-First migrations | LTS release aligned with .NET 10. Named query filters, JSON column support, vector search for SQL Server 2025. Must match .NET 10 exactly. |
| SQL Server | 2022+ | Database | Project requirement. SQL Server 2022 is current mainstream; 2025 adds vector types (EF Core 10 supports both). |
| ASP.NET Core Identity | 10.0 (built-in) | Authentication & authorization | Built into ASP.NET Core. Supports claims-based and policy-based authorization out of the box. Custom `ApplicationUser` with claims storage. |

### Supporting Libraries

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| FluentValidation | 12.x | Model validation | Use in Application layer for request/command validation. Replaces DataAnnotations for complex rules. Integrates with ASP.NET validation pipeline. |
| AutoMapper | 16.2.0 | Object-to-object mapping | Use in Application layer for Entity ↔ DTO/ViewModel mapping. **Note:** v13+ requires commercial license for enterprises. For this project (non-enterprise), v16 is fine. |
| MediatR | 14.2.0 | Mediator / CQRS pattern | Use in Application layer for command/query separation. Optional — adds complexity but enforces Clean Architecture boundaries. |
| Ardalis.GuardClauses | 4.x | Guard clause utilities | Use in Domain layer for entity invariant validation. Lightweight, no dependencies. |
| Serilog | 4.x | Structured logging | Replace default `ILogger` with structured logging. Enrich with request context, user info. |
| Stripe.net | 48.3.0 | Payment processing | Use in Infrastructure layer for e-commerce checkout. Stripe Checkout (hosted page) is simplest integration. |

### Frontend Technologies

| Technology | Version | Purpose | When to Use |
|------------|---------|---------|-------------|
| Tailwind CSS | v4.x | Utility-first CSS | Use `@tailwindcss/cli` (NOT `tailwindcss` CLI). Input CSS uses `@import "tailwindcss"` + `@source` directives (no `tailwind.config.js`). |
| jQuery | 3.7.x | AJAX + DOM manipulation | Use for AJAX calls, form enhancements, dynamic UI. Aligns with project constraint "Razor Views + jQuery". Deprecated `jquery-ajax-unobtrusive` — use `$.ajax()` directly. |
| libman | latest | Client-side lib management | Alternative to npm for managing jQuery CDN. Use if you want to avoid npm in the Host project. |

### Development Tools

| Tool | Purpose | Notes |
|------|---------|-------|
| xUnit | Unit testing | Standard for ASP.NET Core. Use in test projects per layer. |
| Moq | Mocking | Mock interfaces for unit testing Application/Domain layers. |
| FluentAssertions | Assertion library | Readable test assertions. Pairs with xUnit. |
| Ardalis.CleanArchitecture.Template | Solution template | `dotnet new clean-arch` — provides proven 4-layer structure (Domain, UseCases/Application, Infrastructure, Web/Host). |

## Installation

```bash
# Solution creation (option A — from template)
dotnet new install Ardalis.CleanArchitecture.Template
dotnet new clean-arch -o PetPlatform

# Solution creation (option B — manual, matches PROJECT.md 4-layer structure)
dotnet new sln -n PetPlatform
dotnet new classlib -n PetPlatform.Domain -f net10.0
dotnet new classlib -n PetPlatform.Application -f net10.0
dotnet new classlib -n PetPlatform.Infrastructure -f net10.0
dotnet new mvc -n PetPlatform.Host.MVC -f net10.0

# Core NuGet packages (Infrastructure layer)
dotnet add PetPlatform.Infrastructure package Microsoft.EntityFrameworkCore.SqlServer --version 10.0.10
dotnet add PetPlatform.Infrastructure package Microsoft.EntityFrameworkCore.Tools --version 10.0.10
dotnet add PetPlatform.Infrastructure package Microsoft.AspNetCore.Identity.EntityFrameworkCore --version 10.0.*
dotnet add PetPlatform.Infrastructure package Stripe.net --version 48.3.0
dotnet add PetPlatform.Infrastructure package Serilog.AspNetCore --version 4.*
dotnet add PetPlatform.Infrastructure package Serilog.Sinks.MSSqlServer

# Application layer packages
dotnet add PetPlatform.Application package FluentValidation --version 12.*
dotnet add PetPlatform.Application package FluentValidation.DependencyInjectionExtensions --version 12.*
dotnet add PetPlatform.Application package AutoMapper --version 16.2.*
dotnet add PetPlatform.Application package MediatR --version 14.2.*
dotnet add PetPlatform.Application package Ardalis.GuardClauses --version 4.*

# Domain layer packages
dotnet add PetPlatform.Domain package Ardalis.GuardClauses --version 4.*

# Host.MVC packages
dotnet add PetPlatform.Host.MVC package Microsoft.EntityFrameworkCore.Design --version 10.0.10
dotnet add PetPlatform.Host.MVC package Stripe.net --version 48.3.0

# Frontend (Host.MVC directory)
cd PetPlatform.Host.MVC
npm init -y
npm install -D @tailwindcss/cli tailwindcss
# For jQuery: use libman or CDN, or npm install jquery

# Test projects
dotnet new xunit -n PetPlatform.Domain.Tests -f net10.0
dotnet new xunit -n PetPlatform.Application.Tests -f net10.0
dotnet add package Moq --version 4.*
dotnet add package FluentAssertions --version 7.*
```

## Tailwind CSS v4 Setup (ASP.NET Core MVC)

Tailwind v4 has **breaking changes** from v3. The config file (`tailwind.config.js`) is gone. Use this approach:

```bash
# Install
npm install -D @tailwindcss/cli tailwindcss
```

**Input CSS** (`wwwroot/css/site.css`):
```css
@import "tailwindcss";
@source "../../Views/";
```

**package.json scripts**:
```json
{
  "scripts": {
    "css:build": "@tailwindcss/cli -i ./wwwroot/css/site.css -o ./wwwroot/css/styles.css --minify",
    "css:watch": "@tailwindcss/cli -i ./wwwroot/css/site.css -o ./wwwroot/css/styles.css --watch"
  }
}
```

**.csproj MSBuild integration** (builds CSS before compilation):
```xml
<Target Name="Tailwind" BeforeTargets="Build">
  <Exec Command="npm run css:build" />
</Target>
```

**_Layout.cshtml**:
```html
<link rel="stylesheet" href="~/css/styles.css" asp-append-version="true" />
```

## Alternatives Considered

| Recommended | Alternative | When to Use Alternative |
|-------------|-------------|-------------------------|
| .NET 10 | .NET 8 LTS | If team tools/IDEs don't support .NET 10 yet. .NET 8 EOL Nov 2026. |
| EF Core 10 | Dapper | If raw SQL performance is critical and you don't need change tracking. |
| Tailwind CSS v4 | Tailwind CSS v3 | If v4 `@source` directive has issues with Areas or complex view structures. v3 is stable and well-documented. |
| jQuery 3.x | Alpine.js | If you want a more modern declarative approach for dynamic UI. Alpine.js is lighter and pairs well with Tailwind. |
| Stripe.net | PayPal SDK | If you need PayPal as primary payment method. Stripe is dominant for developer-first integrations. |
| MediatR | Service layer classes | If CQRS adds complexity you don't need. Direct service injection works fine for simpler CRUD apps. |
| AutoMapper | Manual mapping | If mappings are few and simple. Manual mapping gives full control and no magic. |
| Serilog | NLog | Both are excellent. Serilog has better .NET integration and structured logging out of the box. |
| Ardalis.CleanArchitecture.Template | JasonTaylor CleanArchitecture | Both are proven templates. Ardalis is more MVC-focused; JasonTaylor leans toward Angular/React. |

## What NOT to Use

| Avoid | Why | Use Instead |
|-------|-----|-------------|
| `tailwindcss` npm package (standalone) | In v4, the CLI is in `@tailwindcss/cli`. Using `tailwindcss` directly won't work for builds. | `@tailwindcss/cli` |
| `Microsoft.jQuery.Unobtrusive.Ajax` | Deprecated, in maintenance mode only. No new features. | jQuery `$.ajax()` directly, or fetch API |
| IdentityServer4 / Duende IdentityServer | Complex, commercial licensing for production. Overkill for server-rendered MVC with cookie auth. | ASP.NET Core Identity with cookie authentication |
| Repository + Unit of Work over EF Core | EF Core's `DbContext` already implements Unit of Work and `DbSet` acts as Repository. Adds unnecessary abstraction. | Use `DbContext` directly in Application layer services |
| `DataAnnotations` for validation | Limited, scattered across models, hard to test. | FluentValidation with rules in separate validator classes |
| AutoMapper >= 13 (for commercial use) | v13+ requires commercial license for companies with revenue > $1M. | AutoMapper <= 12, or Mapster, or manual mapping |
| Swashbuckle / Swagger | Replaced by built-in OpenAPI in .NET 9+. | `Microsoft.AspNetCore.OpenApi` (ships with .NET 10) |
| `dotnet new webapi` for MVC | Wrong template. Generates API-only, no views. | `dotnet new mvc` |

## Stack Patterns by Variant

**If team is new to Clean Architecture:**
- Start with 3-layer (Domain, Application, Host.MVC) — skip Infrastructure until you need it
- Use Ardalis.CleanArchitecture.Template for structure guidance
- Add Infrastructure layer when you need file storage, email, or external APIs

**If building MVP rapidly:**
- Skip MediatR and AutoMapper initially — use direct service injection and manual mapping
- Add them when the codebase grows and you need CQRS or complex mappings
- Use Tailwind v3 if v4 integration proves problematic

**If payment is not core to v1:**
- Defer Stripe.net installation until e-commerce phase
- Focus on Identity + CRUD features first

**If Arabic RTL support is needed (project constraint):**
- Add `dir="rtl"` to `<html>` tag
- Tailwind v4 has native RTL support via `rtl:` variant — no extra plugins needed
- Test all layouts with RTL from Phase 1

## Version Compatibility

| Package | Compatible With | Notes |
|---------|-----------------|-------|
| EF Core 10.0.x | .NET 10 only | EF Core major version MUST match .NET runtime version exactly |
| FluentValidation 12.x | .NET 8+ | Safe to use with .NET 10 |
| AutoMapper 16.x | .NET 8+ | License changed at v13 — check if commercial use applies |
| MediatR 14.x | .NET 8+ | Requires `Microsoft.Extensions.DependencyInjection` (built into ASP.NET Core) |
| Stripe.net 48.x | .NET 8+ (build), .NET Standard 2.0+ (runtime) | Compatible with .NET 10 |
| Serilog 4.x | .NET 8+ | Compatible with .NET 10 |
| @tailwindcss/cli 4.x | Node.js 18+ | Requires npm, not compatible with libman |

## Sources

- [Microsoft .NET 10 Download Page](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) — Verified SDK 10.0.301, ASP.NET Core Runtime 10.0.9 (HIGH)
- [EF Core Releases](https://learn.microsoft.com/en-us/ef/core/what-is-new) — Verified EF Core 10.0.10 latest stable, LTS until Nov 2028 (HIGH)
- [EF Core 10 What's New](https://github.com/dotnet/EntityFramework.Docs/blob/main/entity-framework/core/what-is-new/ef-core-10.0/whatsnew.md) — Named query filters, JSON columns, vector search (HIGH)
- [ASP.NET Core Identity Claims Auth](https://learn.microsoft.com/en-us/aspnet/core/mvc/security/authorization/claims) — Claims-based auth on ASP.NET Core 10 (HIGH)
- [Policy-Based Auth in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/policies) — AddAuthorizationBuilder, requirements, handlers (HIGH)
- [Tailwind CSS v4 ASP.NET MVC Setup](https://github.com/tailwindlabs/tailwindcss/discussions/17309) — v3→v4 migration guide for MVC (MEDIUM)
- [Tailwind v4 + ASP.NET Core 10 MVC](https://www.stellaradmin.com/blog/add-tailwind4-aspnet-core-10-mvc-razor-pages) — @source directive, MSBuild integration (HIGH)
- [Ardalis Clean Architecture Template](https://github.com/ardalis/CleanArchitecture) — 18K+ stars, .NET 9/10, proven template (HIGH)
- [Stripe.net NuGet](https://www.nuget.org/packages/Stripe.net/) — v48.3.0 stable, .NET 8+ (HIGH)
- [FluentValidation Docs](http://docs.fluentvalidation.net/) — v12 supports .NET 8+ (HIGH)
- [AutoMapper Releases](https://www.jimmybogard.com/) — v16.2.0 released Jul 2026, license changed at v13 (MEDIUM)
- [MediatR Releases](https://www.jimmybogard.com/) — v14.2.0 released Jul 2026 (MEDIUM)
- [jQuery in 2025](https://www.docker.com/blog/why-i-still-use-jquery-2025/) — Still used in 77.8% of top 10M sites, viable for MVC (MEDIUM)
- [ASP.NET Core MVC Anti-Patterns](https://dev.to/mashrulhaque/how-to-design-a-maintainable-net-solution-structure-for-growing-teams-284n) — Common Clean Architecture mistakes (MEDIUM)

---
*Stack research for: ASP.NET Core MVC Pet Platform (Clean Architecture)*
*Researched: 2026-07-18*
