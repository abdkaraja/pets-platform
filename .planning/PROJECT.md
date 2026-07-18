# Pet Platform

## What This Is

A comprehensive pet platform web application built with ASP.NET Core MVC following Clean Architecture. It serves pet owners, veterinarians, and service providers with features spanning authentication, e-commerce, adoption, lost pets, and digital medical records.

## Core Value

Pet owners can manage their pets' complete lifecycle — from purchasing supplies to medical records to adoption — all in one integrated platform.

## Requirements

### Validated

(None yet — ship to validate)

### Active

- [ ] Clean Architecture project structure with 4 layers (Domain, Application, Infrastructure, Host.MVC)
- [ ] ASP.NET Core Identity with roles (Admin, Customer, Vet, ServiceProvider) and Claims-based permissions
- [ ] Customer profile management with My Account page
- [ ] Admin dashboard foundation (user management, role/permission management)
- [ ] E-commerce module (products, cart, orders, inventory)
- [ ] Adoption section
- [ ] Lost animals section
- [ ] Digital medical records with vet dashboard

### Out of Scope

- Mobile native app — web-first approach, mobile later
- Real-time chat — high complexity, not core to v1
- Multi-language support — Arabic only for v1

## Context

- **Architecture**: Clean Architecture with strict dependency rule (Host.MVC → Infrastructure → Application → Domain)
- **Auth pattern**: Claims-based Permissions layered on top of ASP.NET Core Identity roles, enabling granular permission management without code changes
- **Frontend**: Razor Views + jQuery + Tailwind CSS (build pipeline via npm/Tailwind CLI)
- **Database**: SQL Server with EF Core Code-First migrations
- **Admin dashboard**: Foundation in Phase 1, expands with each subsequent phase
- **Vet dashboard**: Built in Phase 5, leverages existing auth + medical records

## Constraints

- **Tech Stack**: ASP.NET Core MVC (latest LTS), EF Core, SQL Server — team familiarity drives this choice
- **Architecture**: Clean Architecture with 4 layers — strict dependency rule enforced
- **Auth**: Must use Claims-based Permissions (not just roles) for future flexibility
- **Frontend**: Razor Views + jQuery + Tailwind CSS — no SPA framework
- **Phasing**: Admin dashboard foundation in Phase 1, expands incrementally

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Clean Architecture (4 layers) | Separation of concerns, testability, maintainability | — Pending |
| Claims-based Permissions over Identity roles | Granular permission management without code changes | — Pending |
| Admin Dashboard foundation in Phase 1 | Tightly coupled with user/role management | — Pending |
| Vet Dashboard in Phase 5 | Depends on auth + medical records + account system | — Pending |
| Razor Views + jQuery (no SPA) | Simplicity, server-rendered, no build complexity | — Pending |

## Evolution

This document evolves at phase transitions and milestone boundaries.

**After each phase transition** (via `/gsd-transition`):
1. Requirements invalidated? → Move to Out of Scope with reason
2. Requirements validated? → Move to Validated with phase reference
3. New requirements emerged? → Add to Active
4. Decisions to log? → Add to Key Decisions
5. "What This Is" still accurate? → Update if drifted

**After each milestone** (via `/gsd-complete-milestone`):
1. Full review of all sections
2. Core Value check — still the right priority?
3. Audit Out of Scope — reasons still valid?
4. Update Context with current state

---
*Last updated: 2026-07-18 after initialization*
