# Roadmap: Pet Platform

## Overview

The Pet Platform delivers a comprehensive pet lifecycle management system in 5 phases. Phase 1 establishes the Clean Architecture foundation, authentication with 4 roles, pet profiles, customer accounts, and admin dashboard foundation. Phase 2 builds the full e-commerce module (catalog, cart, checkout, orders, inventory). Phase 3 adds the adoption system with listings, search, and application workflows. Phase 4 delivers the lost pets module with reporting and search. Phase 5 completes v1 with digital medical records and admin expansion. The vet dashboard is deferred to v2.

## Phases

**Phase Numbering:**
- Integer phases (1, 2, 3): Planned milestone work
- Decimal phases (2.1, 2.2): Urgent insertions (marked with INSERTED)

Decimal phases appear between their surrounding integers in numeric order.

- [ ] **Phase 1: Foundation, Identity & Core Profiles** - Clean Architecture scaffold, ASP.NET Core Identity with 4 roles + claims, pet profiles, customer accounts, admin dashboard foundation
- [ ] **Phase 2: E-Commerce Module** - Product catalog, shopping cart, checkout with payment, order management, inventory
- [ ] **Phase 3: Adoption Module** - Adoptable pet listings, search/filter, adoption applications, shelter review workflow
- [ ] **Phase 4: Lost Pets Module** - Report lost/found pets, search by species/breed/location, basic alerts for matches
- [ ] **Phase 5: Medical Records & Admin Expansion** - Digital vaccination history, medication records, vet visit notes, expanded admin tools

## Phase Details

### Phase 1: Foundation, Identity & Core Profiles
**Goal**: Users can create accounts, manage their pets, and admins can manage users and roles — the entire platform runs on this foundation
**Depends on**: Nothing (first phase)
**Requirements**: AUTH-01, AUTH-02, AUTH-03, AUTH-04, AUTH-05, AUTH-06, AUTH-07, AUTH-08, PET-01, PET-02, PET-03, PET-04, PET-05, PET-06, ADMN-01, ADMN-02, ADMN-03, ADMN-04, ADMN-05
**Success Criteria** (what must be TRUE):
  1. User can sign up with email/password, verify email, and log in across browser sessions
  2. User can create, view, edit, and delete pet profiles — supporting multiple pets with name, species, breed, age, weight, and photo
  3. User can view and edit their customer profile (address, phone, city, notification preferences) via a My Account page
  4. Admin can view all users, activate/deactivate accounts, assign roles, create roles, and assign permissions to roles
  5. Four roles (Admin, Customer, Vet, ServiceProvider) exist with claims-based permissions enforced via policy-based authorization
**Plans**: TBD
**UI hint**: yes

### Phase 2: E-Commerce Module
**Goal**: Users can browse products, add to cart, checkout with payment, and track orders — admins manage the full product lifecycle
**Depends on**: Phase 1
**Requirements**: ECOM-01, ECOM-02, ECOM-03, ECOM-04, ECOM-05, ECOM-06, ECOM-07, ECOM-08
**Success Criteria** (what must be TRUE):
  1. User can browse a product catalog with search and filter by pet type, category, price, and brand
  2. User can add products to a shopping cart, checkout with payment, and receive order confirmation
  3. User can view order history and track order status
  4. Admin can manage products (CRUD), categories, and inventory levels
**Plans**: TBD
**UI hint**: yes

### Phase 3: Adoption Module
**Goal**: Users can discover adoptable pets, submit applications, and shelters can review and manage adoption workflows
**Depends on**: Phase 1
**Requirements**: ADPT-01, ADPT-02, ADPT-03, ADPT-04, ADPT-05
**Success Criteria** (what must be TRUE):
  1. User can browse adoptable pet listings with search and filter by species, breed, age, size, and location
  2. User can submit an adoption application form with household information and pet experience
  3. Shelter can review, approve, or reject adoption applications from a unified dashboard
  4. Adopter receives status updates at each stage of the application process
**Plans**: TBD
**UI hint**: yes

### Phase 4: Lost Pets Module
**Goal**: Users can report lost or found pets and discover matches through search — creating community-driven pet recovery
**Depends on**: Phase 1
**Requirements**: LOST-01, LOST-02, LOST-03
**Success Criteria** (what must be TRUE):
  1. User can report a lost or found pet with details (species, breed, color, location, date, description)
  2. User can search lost pet reports by species, breed, color, location, and date range
  3. User receives basic alerts when new lost pet reports match their area or previously reported pets
**Plans**: TBD
**UI hint**: yes

### Phase 5: Medical Records & Admin Expansion
**Goal**: Vets can record pet health data digitally and pet owners can view their pet's complete medical history
**Depends on**: Phase 1, Phase 3
**Requirements**: MED-01, MED-02, MED-03, MED-04
**Success Criteria** (what must be TRUE):
  1. Vet can create vaccination history records for assigned pets
  2. Vet can create medication records and visit notes for assigned pets
  3. Pet owner can view their pet's complete medical history (vaccinations, medications, visits)
**Plans**: TBD
**UI hint**: yes

## Progress

**Execution Order:**
Phases execute in numeric order: 1 → 2 → 3 → 4 → 5

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Foundation, Identity & Core Profiles | 0/0 | Not started | - |
| 2. E-Commerce Module | 0/0 | Not started | - |
| 3. Adoption Module | 0/0 | Not started | - |
| 4. Lost Pets Module | 0/0 | Not started | - |
| 5. Medical Records & Admin Expansion | 0/0 | Not started | - |
