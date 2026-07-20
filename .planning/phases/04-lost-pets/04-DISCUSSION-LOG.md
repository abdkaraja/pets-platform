# Phase 4: Lost Pets Module - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-07-20
**Phase:** 4-Lost Pets Module
**Areas discussed:** Lost pet report model, Matching & alerts, Location handling, Report lifecycle

---

## Lost pet report model

| Option | Description | Selected |
|--------|-------------|----------|
| Separate entity (Recommended) | New LostPetReport entity with optional PetId FK — reports can exist without a linked Pet | ✓ |
| Extend Pet entity | Add lost/found fields directly to Pet — simpler but couples concerns | |

**User's choice:** Separate entity (Recommended)
**Notes:** Follows same pattern as AdoptionListing from Phase 3

---

| Option | Description | Selected |
|--------|-------------|----------|
| Enum field (Recommended) | LostPetReportType enum (Lost/Found) on the entity — simple, extensible | ✓ |
| Separate entities | LostPetReport and FoundPetReport — more flexibility but more code | |

**User's choice:** Enum field (Recommended)
**Notes:** None

---

| Option | Description | Selected |
|--------|-------------|----------|
| Minimal required (Recommended) | Required: species, color, location, date, description. Optional: breed, photo, petId link | ✓ |
| Comprehensive required | Required: species, breed, color, location, date, description, contact info | |

**User's choice:** Minimal required (Recommended)
**Notes:** Keeps barrier to reporting low

---

| Option | Description | Selected |
|--------|-------------|----------|
| Single photo optional (Recommended) | One photo max, optional — reuses existing IFileStorageService | |
| Multiple photos optional | Allow 1-5 photos — more helpful for identification but adds complexity | ✓ |
| No photos | Text-only reports — simplest but less effective | |

**User's choice:** Multiple photos optional
**Notes:** 1-5 photos for better identification

---

## Matching & alerts

| Option | Description | Selected |
|--------|-------------|----------|
| In-app notifications (Recommended) | Show matches in a dashboard/notification center — no external dependencies | ✓ |
| Email notifications | Send emails via SMTP — reaches users outside the app | |
| In-app + Email | Both channels — maximum reach but most complex | |

**User's choice:** In-app notifications (Recommended)
**Notes:** Simpler, no email infrastructure needed

---

| Option | Description | Selected |
|--------|-------------|----------|
| Species + location (Recommended) | Same species + same city/area — broad enough to catch potential matches | ✓ |
| Species + breed + location | Same species + similar breed + same city — more precise | |
| Species + location + date | Time-sensitive but complex | |

**User's choice:** Species + location (Recommended)
**Notes:** Broad enough, simple to implement

---

| Option | Description | Selected |
|--------|-------------|----------|
| Reporter only (Recommended) | Only the reporter gets notified when a potential match is found | ✓ |
| Reporter + public feed | Anyone can browse matches | |
| Reporter + nearby users | Requires location-based user targeting | |

**User's choice:** Reporter only (Recommended)
**Notes:** Private, simple

---

| Option | Description | Selected |
|--------|-------------|----------|
| On report creation (Recommended) | When a new report is created, system checks for existing opposite-type reports | ✓ |
| Background job | Periodic background job checks all open reports | |
| Creation + background | Both — immediate check + periodic sweep | |

**User's choice:** On report creation (Recommended)
**Notes:** Simple, synchronous

---

## Location handling

| Option | Description | Selected |
|--------|-------------|----------|
| Free-text city/area (Recommended) | Simple text field for city/area — low barrier, easy LIKE queries | ✓ |
| Structured city + address | Structured city dropdown + optional address | |
| Coordinates + radius | Lat/lng coordinates + radius search — most precise but complex | |

**User's choice:** Free-text city/area (Recommended)
**Notes:** Low barrier to entry

---

| Option | Description | Selected |
|--------|-------------|----------|
| Dedicated search page (Recommended) | Filter by species, breed, color, location, date range — reuses adoption pattern | ✓ |
| Keyword search | Simple keyword search box | |
| Filters + keyword | Both — dedicated filters + keyword fallback | |

**User's choice:** Dedicated search page (Recommended)
**Notes:** Reuses adoption listing pattern

---

| Option | Description | Selected |
|--------|-------------|----------|
| Exact match | Exact text match — simplest | |
| Contains/partial (Recommended) | Contains/partial match — more forgiving | ✓ |
| Exact + partial toggle | Both exact and partial as filter options | |

**User's choice:** Contains/partial (Recommended)
**Notes:** Catches typos and variations

---

## Report lifecycle

| Option | Description | Selected |
|--------|-------------|----------|
| Open/Resolved (Recommended) | Simple, covers the main use case | ✓ |
| Open/In Review/Resolved | More stages but more tracking | |
| Open/Resolved/Expired | Adds time-based cleanup | |

**User's choice:** Open/Resolved (Recommended)
**Notes:** Simple, covers main use case

---

| Option | Description | Selected |
|--------|-------------|----------|
| Reporter only (Recommended) | Only the original reporter can mark their report as resolved | ✓ |
| Reporter + admins | Admins can close stale reports | |
| Anyone | Anyone can mark resolved — community-driven but risky | |

**User's choice:** Reporter only (Recommended)
**Notes:** Prevents unauthorized changes

---

| Option | Description | Selected |
|--------|-------------|----------|
| No expiry (Recommended) | Reports stay open until manually resolved, simplest | ✓ |
| Auto-close 30 days | Auto-close after 30 days | |
| Prompt after 30 days | Prompt reporter to confirm after 30 days | |

**User's choice:** No expiry (Recommended)
**Notes:** Simplest approach

---

| Option | Description | Selected |
|--------|-------------|----------|
| Editable when Open (Recommended) | Reporter can edit while report is Open | ✓ |
| No editing | No editing after creation | |
| Admin edit only | Editable only by admins | |

**User's choice:** Editable when Open (Recommended)
**Notes:** Allows updating details

---

| Option | Description | Selected |
|--------|-------------|----------|
| Public to all users (Recommended) | All users can browse/search all open reports | ✓ |
| Private (reporter + matches only) | Only the reporter and matched users see reports | |
| Public with hidden contact | Public reports but contact info hidden | |

**User's choice:** Public to all users (Recommended)
**Notes:** Maximum visibility for community recovery

---

## Agent's Discretion

- Pagination, sorting, and search UX approach — planner decides
- Notification center implementation details — planner decides
- Photo upload UI for multiple images — planner decides
- EF Core configurations and migrations — planner decides

## Deferred Ideas

None — discussion stayed within phase scope.
