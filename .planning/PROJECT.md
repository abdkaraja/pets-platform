# Pet Platform

## What This Is

A comprehensive pet platform web application built with ASP.NET Core MVC following Clean Architecture. It serves pet owners, veterinarians, and service providers with features spanning authentication, e-commerce, adoption, lost pets, and digital medical records.

## Core Value

Pet owners can manage their pets' complete lifecycle — from purchasing supplies to medical records to adoption — all in one integrated platform.

## Current Milestone: v1.1 Vet Dashboard & Reminders

**Goal:** Complete the veterinary dashboard with SOAP notes, treatment plans, prescription management, and add reminders for pet owners

**Target features:**
- SOAP notes (Subjective, Objective, Assessment, Plan)
- Treatment plans for pets
- Prescription management (dosage, frequency, duration)
- Vaccination reminders for pet owners
- Medication reminders for pet owners
- Reminder preferences configuration

## Requirements

### Validated

- ✓ Clean Architecture project structure with 4 layers (Domain, Application, Infrastructure, Host.MVC) — v1.0
- ✓ ASP.NET Core Identity with roles (Admin, Customer, Vet, ServiceProvider) and Claims-based permissions — v1.0
- ✓ Customer profile management with My Account page — v1.0
- ✓ Admin dashboard foundation (user management, role/permission management) — v1.0
- ✓ E-commerce module (products, cart, orders, inventory) — v1.0
- ✓ Adoption section — v1.0
- ✓ Lost animals section — v1.0
- ✓ Digital medical records with vet dashboard foundation — v1.0

### Active

- [ ] Vet Dashboard with SOAP notes
- [ ] Treatment plans for pets
- [ ] Prescription management
- [ ] Vaccination reminders
- [ ] Medication reminders
- [ ] Reminder preferences

### Out of Scope

- Mobile native app — web-first approach, mobile later
- Real-time chat — high complexity, not core to v1
- Multi-language support — Arabic only for v1
- Emergency Passport — deferred to future milestone
- Digital Contracts — deferred to future milestone

## Context

- **Architecture**: Clean Architecture with strict dependency rule (Host.MVC → Infrastructure → Application → Domain)
- **Auth pattern**: Claims-based Permissions layered on top of ASP.NET Core Identity roles, enabling granular permission management without code changes
- **Frontend**: Razor Views + jQuery + Tailwind CSS (build pipeline via npm/Tailwind CLI)
- **Database**: SQL Server with EF Core Code-First migrations
- **Admin dashboard**: Foundation in Phase 1, expands with each subsequent phase
- **Vet dashboard**: Built in Phase 5, leverages existing auth + medical records
- **v1.0 shipped**: 5 phases, 15 plans, 266 files, ~14,700 LOC

## Constraints

- **Tech Stack**: ASP.NET Core MVC (latest LTS), EF Core, SQL Server — team familiarity drives this choice
- **Architecture**: Clean Architecture with 4 layers — strict dependency rule enforced
- **Auth**: Must use Claims-based Permissions (not just roles) for future flexibility
- **Frontend**: Razor Views + jQuery + Tailwind CSS — no SPA framework
- **Phasing**: Admin dashboard foundation in Phase 1, expands incrementally

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Clean Architecture (4 layers) | Separation of concerns, testability, maintainability | ✓ Good |
| Claims-based Permissions over Identity roles | Granular permission management without code changes | ✓ Good |
| Admin Dashboard foundation in Phase 1 | Tightly coupled with user/role management | ✓ Good |
| Vet Dashboard in Phase 5 | Depends on auth + medical records + account system | ✓ Good |
| Razor Views + jQuery (no SPA) | Simplicity, server-rendered, no build complexity | ✓ Good |
| PaymentService in Infrastructure layer | Stripe SDK dependency violates Clean Architecture if placed in Application | ✓ Good |
| AdoptionApplication dictionary-based status | Flexible workflow, not forward-only arithmetic | ✓ Good |
| LostPetReport separate entity | Optional PetId FK, not on Pet entity | ✓ Good |
| Matching = same species + contains-location | Bidirectional notifications, simple algorithm | ✓ Good |

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
*Last updated: 2026-07-21 after v1.1 milestone start*
