# Phase 3: Adoption Module - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-07-20
**Phase:** 3-Adoption Module
**Areas discussed:** Adoptable pet data model

---

## Adoptable Pet Data Model

### Entity Design

| Option | Description | Selected |
|--------|-------------|----------|
| Separate AdoptionListing entity | New entity with Pet reference, shelter info, listing status, location, description. Pet stays clean as a user-owned profile. | ✓ |
| Extend Pet with status | Add IsAvailableForAdoption, ShelterId, ListingStatus fields to Pet. Simpler but mixes concerns. | |
| You decide | Agent picks the cleanest approach based on codebase patterns. | |

**User's choice:** Separate AdoptionListing entity
**Notes:** Follows separation of concerns. Pet remains user-owned; listing has its own lifecycle.

### Listing Fields

| Option | Description | Selected |
|--------|-------------|----------|
| Location + description + status | Shelter/location, description text, listing status (Active/Adopted/Closed). Minimal — most info from linked Pet. | ✓ |
| Rich listing profile | Location, description, photos, vaccination status, temperament notes, adoption fee, special requirements. | |
| You decide | Agent picks what's essential for v1 adoption workflow. | |

**User's choice:** Location + description + status
**Notes:** Minimal listing data for v1. Pet entity provides the rich profile data.

### Shelter Identity

| Option | Description | Selected |
|--------|-------------|----------|
| Shelter = ServiceProvider role | ServiceProvider users manage listings. Their CustomerProfile doubles as shelter profile. Reuses existing role. | ✓ |
| Shelter = new concept | New Shelter entity (name, address, contact) linked to users. More flexible but adds a new domain concept. | |
| You decide | Agent picks the simplest approach for v1. | |

**User's choice:** Shelter = ServiceProvider role
**Notes:** No new role needed. Existing ServiceProvider role + CustomerProfile handles shelter identity.

---

## the agent's Discretion

- Application form field details (household info, pet experience questions)
- Search/filter UX approach (reuse catalog pattern or different)
- Notification mechanism for status updates
- Pagination, sorting, and location handling for listings

## Deferred Ideas

None — discussion stayed within phase scope.
