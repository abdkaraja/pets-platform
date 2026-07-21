# Feature Research

**Domain:** Veterinary dashboard & pet-owner reminders
**Researched:** 2026-07-21
**Confidence:** HIGH

## Existing Codebase Baseline

v1.0 already provides the foundation these features build on:

| Existing Entity | What It Has | What's Missing for v1.1 |
|----------------|-------------|-------------------------|
| `VetVisitNote` | SOAP fields (Subjective, Objective, Assessment, Plan) | No dashboard UI; notes aren't surfaced to vets as a workflow; no link to treatment plans |
| `VaccinationRecord` | Vaccine name, date administered, batch lot, `NextDueDate` | `NextDueDate` exists but nothing consumes it — no reminder generation |
| `MedicationRecord` | Medication name, dosage, frequency, start/end dates, instructions | No prescription number, no refill tracking, no reminder generation from frequency |
| `CustomerProfile` | `NotificationPreferences` (single boolean) | Too coarse — can't toggle vaccination vs medication reminders independently |
| `VetAssignment` | Pet-to-vet binding with Pending/Accepted/Rejected workflow | No vet-side dashboard to view assigned pets and their records |

**Key insight:** The data model is 70% there. The gap is (1) vet-side dashboard UI surfacing existing records, (2) treatment plans as a new entity, (3) prescriptions as a new entity, (4) a reminder engine consuming existing date fields, and (5) granular reminder preferences.

---

## Feature Landscape

### Table Stakes (Users Expect These)

Features vets and pet owners assume exist. Missing = product feels broken.

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| **Vet Dashboard: Assigned Pets List** | Vets need to see which pets they're responsible for before doing anything else | LOW | Leverages existing `VetAssignment` + `VetProfile`. Simple filtered list view. |
| **Vet Dashboard: Create/Edit SOAP Notes** | Core vet workflow — without this the vet role is useless | MEDIUM | `VetVisitNote` entity already has SOAP fields. Need CRUD controller, Razor views, and form with 4 textarea sections. |
| **Vet Dashboard: View Pet Medical History** | Vets need context — past vaccinations, medications, and visit notes — before writing new notes | MEDIUM | Merge existing `VaccinationRecord`, `MedicationRecord`, `VetVisitNotes` into a unified timeline view per pet. Admin `MedicalRecords` action already does something similar. |
| **Treatment Plans: Create and Track** | Vets write treatment plans for ongoing cases — surgery recovery, chronic conditions, dental work | MEDIUM | New entity `TreatmentPlan` with title, diagnosis, treatment steps, goals, status (Active/Completed/Cancelled), linked to Pet + Vet. |
| **Prescription Management** | Vets issue formal prescriptions distinct from casual medication notes | MEDIUM | New entity `Prescription` with prescription number, medication, dosage, frequency, duration, validity period, refill count. Links to VetVisitNote or TreatmentPlan. |
| **Vaccination Reminders for Owners** | Pet owners expect to be reminded before vaccines are due — `NextDueDate` exists for exactly this | MEDIUM | Background service or scheduled task that checks `VaccinationRecord.NextDueDate`, generates in-app reminders. Follows existing `MatchNotification` pattern. |
| **Medication Reminders for Owners** | Owners need reminders for ongoing medications — give pill X every 12 hours | MEDIUM | Consumes `MedicationRecord.Frequency` + `StartDate`/`EndDate` to generate recurring reminder instances. |
| **Reminder Preferences (Granular)** | Owners must control what they're reminded about — not just on/off | LOW | Extend `CustomerProfile.NotificationPreferences` from `bool` to a proper preferences entity or multi-flag model (vaccination on, medication on, frequency preferences). |

### Differentiators (Competitive Advantage)

Features that set this platform apart from basic vet record systems.

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| **Unified Pet Timeline** | Showing all medical events (SOAP, prescriptions, vaccinations, medications) in chronological order per pet gives vets instant clinical context — most vet software silos these | MEDIUM | Combine all record types into a single timeline widget on the vet dashboard. |
| **Prescription-to-Reminder Bridge** | When a vet issues a prescription, medication reminders auto-generate — zero manual setup by the owner | HIGH | Requires linking `Prescription` creation to reminder engine. Owner confirms/adjusts reminders. |
| **Treatment Plan Progress Tracking** | Vets can mark treatment steps complete, see what's done vs pending — accountability and follow-through | MEDIUM | Treatment plan with ordered steps, each with status. Dashboard shows progress bar. |
| **Vet Notes Linked to Prescriptions** | A prescription created during a visit is traceable back to the SOAP note that prompted it — audit trail | LOW | Foreign key from `Prescription` to `VetVisitNote`. Useful for liability and context. |

### Anti-Features (Commonly Requested, Often Problematic)

| Feature | Why Requested | Why Problematic | Alternative |
|---------|---------------|-----------------|-------------|
| **SMS/Email/Push Notifications** | "Reminders should notify owners!" | Requires email service (SendGrid), SMS provider (Twilio), push infrastructure — massive complexity for v1.1. Also needs owner consent flows, unsubscribe management, deliverability. | In-app notification system first (same pattern as `MatchNotification`). Add email/SMS in v1.2 after core reminder logic is validated. |
| **Prescription PDF Generation** | "Vets need printable prescriptions" | Requires PDF library (QuestPDF/iTextSharp), letterhead templates, vet signature image — separate concern from core CRUD. | Store prescriptions as structured data. PDF export is a v2 feature — defer to when vet workflow is stable. |
| **Drug Interaction Checking** | "Flag dangerous drug combinations" | Requires a comprehensive drug database (NDC codes, species-specific interactions). This is a full integration project, not a feature. | Manual vet entry with common-sense warnings (text note: "Review for interactions"). Real drug checking is a v2/v3 feature. |
| **Multi-Vet Collaborative Notes** | "Multiple vets should collaborate on SOAP notes" | Versioning, conflict resolution, attribution complexity. Overkill for v1 where a pet has one assigned vet. | One vet per SOAP note (already enforced by `VetAssignment` one-active-vet rule). Collaboration is a v2 feature. |
| **Automated Prescription Renewal** | "Auto-renew prescriptions" | Legal liability — automated prescribing without vet review is dangerous and potentially illegal depending on jurisdiction. | Manual renewal flow: system shows "Prescription X is expiring" → vet reviews → creates new prescription. |
| **Complex Reminder Scheduling UI** | "Let owners set custom reminder schedules" | Over-engineering — most owners want simple on/off, not cron expressions. Complex scheduling UI is confusing and rarely used. | Simple preferences: on/off per category + reminder lead time (e.g., "remind 7 days before vaccination due date"). |

---

## Feature Dependencies

```
[Vet Dashboard: Assigned Pets List]
    └──requires──> [VetAssignment] (existing)
    └──requires──> [VetProfile] (existing)

[Vet Dashboard: SOAP Notes CRUD]
    └──requires──> [Vet Dashboard: Assigned Pets List]
    └──requires──> [VetVisitNote] (existing entity)
    └──requires──> [Vet Assignment check: vet must be assigned to pet]

[Vet Dashboard: Pet Medical History]
    └──requires──> [Vet Dashboard: Assigned Pets List]
    └──requires──> [VaccinationRecord] (existing)
    └──requires──> [MedicationRecord] (existing)
    └──requires──> [VetVisitNote] (existing)

[Treatment Plans]
    └──requires──> [Vet Dashboard: Assigned Pets List]
    └──requires──> [VetVisitNote] (optional link — plan may come from visit or standalone)

[Prescriptions]
    └──requires──> [Vet Dashboard: Assigned Pets List]
    └──requires──> [MedicationRecord] (prescription extends medication concept)
    └──enhances──> [Vet Dashboard: SOAP Notes CRUD] (prescription linked to visit note)

[Reminder Engine]
    └──requires──> [VaccinationRecord.NextDueDate] (existing field)
    └──requires──> [MedicationRecord.Frequency + StartDate/EndDate] (existing fields)
    └──requires──> [Reminder Preferences] (granular on/off)

[Reminder Preferences]
    └──requires──> [CustomerProfile] (existing entity, extend NotificationPreferences)

[Prescription-to-Reminder Bridge]
    └──requires──> [Prescriptions]
    └──requires──> [Reminder Engine]
    └──enhances──> [Vet Dashboard: SOAP Notes CRUD]
```

### Dependency Notes

- **SOAP Notes CRUD requires Assigned Pets List:** Vets can only write notes for pets they're assigned to. The dashboard must show assigned pets first.
- **Treatment Plans are standalone or visit-linked:** A treatment plan might emerge from a SOAP note visit OR be created independently (e.g., "ongoing dental treatment"). Don't force a visit-note link.
- **Prescriptions extend MedicationRecord conceptually:** `MedicationRecord` is a historical record of what was given. `Prescription` is a formal directive from a vet. They share fields but serve different purposes. A prescription should auto-create a medication record upon fulfillment.
- **Reminder Engine consumes existing fields:** `VaccinationRecord.NextDueDate` and `MedicationRecord.Frequency` already exist. The engine is pure logic — no new entity data needed to START reminders. But the engine needs to track which reminders have been generated/shown (new `Reminder` entity).
- **Prescription-to-Reminder Bridge is a differentiator, not table stakes:** Build prescriptions and reminders independently first. Linking them is an enhancement.

---

## MVP Definition

### Launch With (v1.1)

- [ ] **Vet Dashboard: Assigned Pets + SOAP Notes CRUD** — The core vet experience. Without this, the Vet role is decorative.
- [ ] **Vet Dashboard: Pet Medical History View** — Vets need context before writing notes. Merge existing records into a timeline.
- [ ] **Vaccination Reminders** — `NextDueDate` exists, owners expect reminders. Generate in-app notifications when due dates approach.
- [ ] **Medication Reminders** — `Frequency` + dates exist, generate reminders on the medication schedule.
- [ ] **Granular Reminder Preferences** — Replace boolean `NotificationPreferences` with per-category toggles.

### Add After Validation (v1.1.x)

- [ ] **Treatment Plans** — Important but buildable after vet dashboard core is working. Vets can use the "Plan" field in SOAP notes as a stopgap.
- [ ] **Prescription Management** — Formal prescriptions are important but `MedicationRecord` covers the basics. Build after vet workflow is validated.
- [ ] **Prescription-to-Reminder Bridge** — Enhancement that auto-generates medication reminders from prescriptions. Depends on both systems being stable.

### Future Consideration (v1.2+)

- [ ] **Email/SMS Notifications** — Requires external service integration. In-app reminders first.
- [ ] **Prescription PDF Export** — Vet-facing printable prescriptions. Defer until prescription entity is stable.
- [ ] **Drug Interaction Warnings** — Requires external drug database. Not feasible without a significant integration effort.
- [ ] **Treatment Plan Reminders** — Reminders for follow-up appointments tied to treatment plans. Defer until treatment plans are validated.

---

## Feature Prioritization Matrix

| Feature | User Value | Implementation Cost | Priority |
|---------|------------|---------------------|----------|
| Vet Dashboard: Assigned Pets List | HIGH | LOW | P1 |
| Vet Dashboard: SOAP Notes CRUD | HIGH | MEDIUM | P1 |
| Vet Dashboard: Pet Medical History | HIGH | MEDIUM | P1 |
| Vaccination Reminders | HIGH | MEDIUM | P1 |
| Medication Reminders | HIGH | MEDIUM | P1 |
| Granular Reminder Preferences | MEDIUM | LOW | P1 |
| Treatment Plans | MEDIUM | MEDIUM | P2 |
| Prescription Management | MEDIUM | MEDIUM | P2 |
| Prescription-to-Reminder Bridge | HIGH | HIGH | P2 |
| Unified Pet Timeline (differentiator) | HIGH | MEDIUM | P2 |
| Treatment Plan Progress Tracking | MEDIUM | MEDIUM | P3 |
| Email/SMS Notifications | HIGH | HIGH | P3 |
| Prescription PDF Export | MEDIUM | HIGH | P3 |

**Priority key:**
- P1: Must have for v1.1
- P2: Should have, add when core is working
- P3: Nice to have, future consideration

---

## Competitor Feature Analysis

| Feature | eVetPractice / Cornerstone | Shepherd / Neo | Our Approach |
|---------|---------------------------|----------------|--------------|
| SOAP Notes | Full SOAP with templates, auto-save | Free-form + structured | Structured SOAP (4 fields) without templates — simple, no over-engineering |
| Treatment Plans | Integrated with SOAP, linked to billing | Separate module | Separate entity linked optionally to SOAP — cleaner separation |
| Prescriptions | Full NDC integration, pharmacy routing | Basic prescription entry | Structured data (number, dosage, frequency, duration) — no pharmacy integration |
| Reminders | Email + SMS via integrated services | In-app only | In-app first (MatchNotification pattern), email/SMS deferred |
| Medical History | Full timeline with filtering | Tabbed record view | Unified timeline across record types — differentiator |

---

## Implementation Notes for Roadmap

### Entity Design Guidance

**TreatmentPlan entity should include:**
- `PetId` (required FK)
- `VetUserId` (required — who created it)
- `VetVisitNoteId` (optional FK — may come from a visit)
- `Title` (required — e.g., "Post-surgery ACL recovery")
- `Diagnosis` (required)
- `Goals` (optional — expected outcome)
- `Status` enum: `Active`, `Completed`, `Cancelled`
- `StartDate`, `TargetEndDate`, `CompletedDate`
- `Steps` collection (ordered treatment steps with status)

**Prescription entity should include:**
- `PetId` (required FK)
- `VetUserId` (required — prescribing vet)
- `VetVisitNoteId` (optional FK)
- `PrescriptionNumber` (auto-generated, unique)
- `MedicationName`, `Dosage`, `Frequency`, `DurationDays`
- `StartDate`, `EndDate` (computed from start + duration)
- `RefillsAllowed`, `RefillsUsed`
- `Instructions`, `Notes`
- `Status` enum: `Active`, `Completed`, `Cancelled`, `Expired`

**Reminder entity should include:**
- `PetId` (required FK)
- `OwnerId` (required — who receives it)
- `ReminderType` enum: `Vaccination`, `Medication`, `TreatmentFollowUp`
- `SourceRecordType` + `SourceRecordId` (polymorphic link to VaccinationRecord/MedicationRecord/TreatmentPlan)
- `Title`, `Message`
- `DueDate` (when to remind)
- `Status` enum: `Pending`, `Shown`, `Dismissed`, `Snoozed`, `Expired`
- `SnoozeUntil` (optional — for snooze functionality)

**ReminderPreferences entity should include:**
- `OwnerId` (required FK, 1:1 with CustomerProfile)
- `VaccinationRemindersEnabled` (bool, default true)
- `MedicationRemindersEnabled` (bool, default true)
- `TreatmentFollowUpEnabled` (bool, default true)
- `ReminderLeadDays` (int, default 7 — how many days before due date to start showing)
- `QuietHoursStart`, `QuietHoursEnd` (optional — suppress notifications during sleep hours)

### Controller/Area Structure

New `Vet` area (or expand existing patterns):
- `VetDashboardController` — Assigned pets list, pet detail
- `VetVisitNotesController` — CRUD for SOAP notes (scoped to assigned pets)
- `TreatmentPlansController` — CRUD for treatment plans (scoped to assigned pets)
- `PrescriptionsController` — CRUD for prescriptions (scoped to assigned pets)

Reminders go in the existing `Customer` area:
- Extend `MyAccountController` or create `RemindersController`
- Reminder list, mark as read, snooze, dismiss

### Background Service Pattern

For reminder generation, use `BackgroundService` (ASP.NET Core hosted service):
- Runs on a schedule (e.g., daily at 6 AM)
- Checks `VaccinationRecord.NextDueDate` against current date + `ReminderLeadDays`
- Checks `MedicationRecord` frequency + dates for recurring reminders
- Creates `Reminder` entities for anything approaching due
- Respects `ReminderPreferences` — skip if owner disabled that category
- Idempotent — don't create duplicate reminders for the same source record

---

## Sources

- Existing codebase analysis (Domain entities, Infrastructure services, Controllers, Views)
- `PROJECT.md` — v1.1 milestone scope and existing architecture constraints
- ASP.NET Core MVC patterns established in v1.0 (Clean Architecture, Claims-based auth, Result pattern)
- Veterinary practice management software domain knowledge (SOAP note standards, treatment planning workflows, prescription management conventions)

---
*Feature research for: Vet Dashboard & Reminders*
*Researched: 2026-07-21*
