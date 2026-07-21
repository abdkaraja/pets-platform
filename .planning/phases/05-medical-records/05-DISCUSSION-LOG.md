# Phase 5: Medical Records & Admin Expansion - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-07-21
**Phase:** 5-Medical Records & Admin Expansion
**Areas discussed:** Vet-Pet Assignment, Medical Record Structure, Vet Area Layout, Pet Owner Medical View, Admin Expansion

---

## Vet-Pet Assignment

### How should vets get assigned to pets?

| Option | Description | Selected |
|--------|-------------|----------|
| Admin assigns vets to pets | Admin dashboard has a page to assign vets to specific pets. Vets only see assigned pets. Most controlled approach. | |
| Vet self-assigns from all pets | Vets see all pets in the system and can self-assign. Simple, low friction, but less control. | |
| Owner requests a vet | Owner selects a vet from a list and requests assignment. Vet accepts/rejects. Two-step workflow. | ✓ |
| All vets see all pets | No assignment needed. Any vet can create records for any pet. Simplest, but no accountability. | |

**User's choice:** Owner requests a vet
**Notes:** Two-step workflow with accept/reject provides good accountability.

### When the owner requests a vet, what happens?

| Option | Description | Selected |
|--------|-------------|----------|
| Simple assignment (no accept/reject) | Owner picks a vet, vet is immediately assigned. Vet can see the pet. No rejection flow. | |
| Vet accepts or rejects | Owner sends request, vet sees pending requests and can accept or reject. Vet gets notification. | ✓ |
| Vet accepts with notes | Vet accepts/rejects with optional note (e.g., "I don't treat birds"). More professional. | |

**User's choice:** Vet accepts or rejects
**Notes:** Standard accept/reject flow without notes.

### How does the owner find and select a vet?

| Option | Description | Selected |
|--------|-------------|----------|
| All users with Vet role | Owner sees all users who have the Vet role in a dropdown. Simple, no extra data needed. | |
| Vet profile page | Vets have a profile with specialty, clinic, bio. Owner browses vet profiles and selects. | |
| Search/filter vets | Owner can search vets by name, specialty, location. More discoverable but more UI work. | ✓ |

**User's choice:** Search/filter vets
**Notes:** Needs vet profiles with searchable fields.

### What fields should a vet profile have?

| Option | Description | Selected |
|--------|-------------|----------|
| Basic (name, clinic, specialty) | Name (from user account), Clinic name, Specialty (dropdown: General, Surgery, Dental, etc.). Minimal. | |
| Extended (name, clinic, bio, specialty) | Basic + short bio, years of experience, profile photo. More professional. | |
| Full profile (name, clinic, bio, services, availability) | Extended + services offered, availability schedule. Most complete but most work. | ✓ |

**User's choice:** Full profile (name, clinic, bio, services, availability)
**Notes:** Comprehensive vet profile for discoverability.

---

## Medical Record Structure

### What fields should a vaccination record have?

| Option | Description | Selected |
|--------|-------------|----------|
| Basic (vaccine name, date, vet) | Vaccine name, date administered, administering vet. Simple, covers core need. | |
| Standard (name, date, batch, vet, next due) | Basic + batch/lot number, next due date, clinic. More complete for tracking. | |
| Full (name, date, batch, vet, next due, notes) | Standard + notes/reactions, certificate if any. Most thorough. | ✓ |

**User's choice:** Full (name, date, batch, vet, next due, notes)
**Notes:** Complete vaccination tracking.

### What fields should a medication record have?

| Option | Description | Selected |
|--------|-------------|----------|
| Basic (name, dosage, dates, vet) | Medication name, dosage, start/end date, prescribing vet. Covers core tracking. | |
| Standard (name, dosage, frequency, dates, vet) | Basic + frequency (e.g., "twice daily"), reason/diagnosis. More useful for owners. | |
| Full (name, dosage, frequency, dates, vet, notes) | Standard + instructions, side effects noted, refill info. Most complete. | ✓ |

**User's choice:** Full (name, dosage, frequency, dates, vet, notes)
**Notes:** Complete medication tracking with instructions.

### What fields should a vet visit note have?

| Option | Description | Selected |
|--------|-------------|----------|
| Basic (date, reason, notes, vet) | Visit date, reason for visit, vet notes, vet. Simple clinical record. | |
| Standard (date, reason, diagnosis, treatment, notes) | Basic + diagnosis, treatment plan. More structured for medical tracking. | |
| SOAP format (subjective, objective, assessment, plan) | Standard SOAP note format. Professional medical standard, most structured. | ✓ |

**User's choice:** SOAP format (subjective, objective, assessment, plan)
**Notes:** Professional medical standard.

---

## Vet Area Layout

### How should the vet interface be structured?

| Option | Description | Selected |
|--------|-------------|----------|
| Full Vet area (like Admin) | Separate Areas/Vet/ with sidebar, dashboard, multiple controllers. Professional, scalable. | ✓ |
| Simple pages in Customer area | Vet actions under Customer area with role checks. Less work, but muddled separation. | |
| Vet area with minimal dashboard | Small Areas/Vet/ with just a dashboard and pet list. Focused, not over-engineered. | |

**User's choice:** Full Vet area (like Admin)
**Notes:** Professional, scalable, mirrors Admin area structure.

### What should the vet dashboard show?

| Option | Description | Selected |
|--------|-------------|----------|
| Assigned pets + pending requests | List of assigned pets with quick actions, pending assignment requests to accept/reject. Focused. | |
| Stats + assigned pets + recent activity | Dashboard cards (total pets, pending requests, recent records) + pet list + activity feed. More complete. | |
| Stats + pets + requests + calendar | All above + availability calendar view. Most feature-rich. | ✓ |

**User's choice:** Stats + pets + requests + calendar
**Notes:** Full dashboard with calendar for availability.

---

## Pet Owner Medical View

### How should owners view their pet's medical history?

| Option | Description | Selected |
|--------|-------------|----------|
| Separate Medical History page | Dedicated page per pet showing all records (vaccinations, medications, visits) in a timeline. Clean separation. | |
| Inline on Pet Details page | Medical history tabs/sections on the existing Pet Details page. Keeps everything in one place. | |
| Both (details page + separate full view) | Summary on Pet Details, full history on separate page. Most complete but more UI. | ✓ |

**User's choice:** Both (details page + separate full view)
**Notes:** Summary for quick view, full page for complete history.

### How should the medical history timeline be organized?

| Option | Description | Selected |
|--------|-------------|----------|
| Tabbed by record type | Tabs: Vaccinations, Medications, Visits. Each tab shows a table/timeline of that type. Clean separation. | |
| Unified timeline | All records in chronological order with type badges (Vaccination, Medication, Visit). Shows the full picture. | |
| Both (unified + filtered tabs) | Default unified timeline, with filter tabs to show only one type. Most flexible. | ✓ |

**User's choice:** Both (unified + filtered tabs)
**Notes:** Unified view with filter tabs for flexibility.

---

## Admin Expansion

### What admin expansion is needed for Phase 5?

| Option | Description | Selected |
|--------|-------------|----------|
| Vet assignment management only | Admin can assign vets to pets, view vet profiles. Minimal expansion. | |
| Vet management + medical overview | Admin can manage vet profiles, assign vets, view pet medical records. Moderate expansion. | |
| Full vet admin panel | Admin can manage vets, assignments, view all medical records, generate reports. Most complete. | ✓ |

**User's choice:** Full vet admin panel
**Notes:** Complete admin control over vet management and medical records.

---

## the agent's Discretion

- Pagination, sorting, and table/list UX patterns — planner decides
- Calendar implementation details — planner decides
- Medical record forms layout — planner decides
- EF Core configurations and migrations — planner decides
- PDF/printable view for medical history — planner decides if in scope

## Deferred Ideas

None — discussion stayed within phase scope.
