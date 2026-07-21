# Pitfalls Research

**Domain:** Pet Platform Web Application (ASP.NET Core MVC, Clean Architecture) — Vet Dashboard & Reminders Focus
**Researched:** 2026-07-21
**Confidence:** MEDIUM

## Critical Pitfalls

### Pitfall 1: SOAP Notes as Unstructured Text Dump (Clinical Data Lost)

**What goes wrong:**
SOAP notes are implemented as a single `Text` field (or `Subjective`, `Objective`, `Assessment`, `Plan` as four `string` columns) with no structure. Vets paste free‑form text. Later, the system cannot query “all dogs with ear infections this month,” cannot generate a treatment summary, cannot warn about drug interactions, and cannot produce a clean export for a referring vet. The SOAP note becomes a digital paper trail that nobody can search or analyze.

**Why it happens:**
SOAP is a *mnemonic*, not a schema. Developers hear “Subjective, Objective, Assessment, Plan” and translate it into four text fields. The vet demo looks fine—everyone types a paragraph. But veterinary medicine depends on structured data (diagnoses coded to ICD‑10‑CM or AVMA terms, vitals with units, body‑weight‑based dosage calculations). Without structure, the vet dashboard is just a note‑taking app, not a clinical tool.

**How to avoid:**
- Model SOAP as a **composite entity**: `SoapNote` header (timestamp, vet, pet, visit type) with separate child tables for `SubjectiveNote`, `VitalSigns` (weight_kg, temperature_c, heart_rate, respiratory_rate), `Diagnosis` (coded term, laterality, severity), `AssessmentNote`, and `PlanNote`.
- Store free‑text *and* structured fields; the free‑text is the vet's narrative, the structured fields are what the software queries.
- Provide a lookup list for common diagnoses (start with AVMA’s diagnostic terms or SNOMED‑CT Vet). Allow free‑text when the list doesn’t cover, but flag “uncoded” for later cleanup.
- Use `decimal` for vitals (weight_kg, temperature) — never `string`. Units belong in the column name or a separate `Unit` lookup; mixing “kg” and “lbs” in free text creates dosage errors.
- Start simple: a `SoapNote` entity with `Subjective`, `Objective`, `Assessment`, `Plan` strings *plus* `VitalSigns` as a separate JSON column or table. Extend later. The key is to *capture* structured data from day one, not retrofit.

**Warning signs:**
- Vets complain they can't search old notes by diagnosis.
- The SOAP form has four large text areas and nothing else.
- Vital signs are typed as “Wt 12kg, T 101.2, HR 120” in the Subjective field.
- No plan to ever export medical records to another system.

**Phase to address:**
Phase 0 (domain model for medical records) — the `SoapNote` entity must be designed before any UI is built. Retrofitting structure after months of free‑text notes is a migration nightmare.

---

### Pitfall 2: Treatment Plans as Static Documents (No Link to SOAP or Prescriptions)

**What goes wrong:**
Treatment plans are built as standalone PDFs or static Razor pages. They are not linked to the SOAP note that created them, not linked to the prescriptions that implement them, and not linked to the pet’s medical record timeline. When the vet updates the treatment plan, the old plan disappears. When the owner asks “what was the plan after the last visit?”, there is no audit trail.

**Why it happens:**
Treatment plans feel like “documents” — something you print and hand to the owner. Developers build a “Print Treatment Plan” button and call it done. But a treatment plan is a *workflow*: it has a start date, expected duration, milestones (re‑check in 2 weeks), and linked prescriptions. Without relationships, the plan is disconnected from the clinical narrative.

**How to avoid:**
- Model `TreatmentPlan` as an entity with foreign keys to `Pet`, `Vet`, and the originating `SoapNote`.
- `TreatmentPlan` has a one‑to‑many relationship with `Prescription` (each prescription belongs to a plan) and a one‑to‑many with `TreatmentMilestone` (re‑check dates, lab re‑tests).
- Store plan status: `Active`, `Completed`, `Cancelled`. A pet can have only one `Active` plan per condition at a time.
- Provide a timeline view: Pet → [SOAP Note] → [Treatment Plan] → [Prescriptions] → [Reminders] — each node links to the next.
- The print/export function is *derived* from the entity, not the source of truth.

**Warning signs:**
- Treatment plan has no foreign key to `SoapNote` or `Pet`.
- Prescriptions are not linked to a treatment plan.
- No status field (Active/Completed) on the treatment plan.
- The “treatment plan” is a byte array or PDF blob stored in the database.

**Phase to address:**
Phase 1 (domain model for treatment plans) — must be designed alongside `SoapNote` and `Prescription` entities. The relationships are the architecture.

---

### Pitfall 3: Prescription Dosage Calculations Done in the UI Layer

**What goes wrong:**
The vet enters “10 mg/kg twice daily for 7 days” in a text field. The JavaScript frontend calculates total tablets, but the server never validates the dosage. A typo (“100 mg/kg” instead of “10 mg/kg”) produces a lethal prescription. The system stores the *text* of the dosage, not the *numbers*, so later queries (“show me all dogs on high‑dose steroids”) are impossible. Audit trails show the vet “prescribed 100 mg/kg” with no system warning.

**Why it happens:**
Dosage math feels like a presentation concern. “The vet knows what they’re doing.” But clinical software has a duty to catch arithmetic errors. Without server‑side validation, the system is a note‑pad, not a safety net.

**How to avoid:**
- Model `Prescription` with *structured* fields: `DrugName`, `DosagePerKg` (decimal), `WeightKg` (decimal, pulled from the pet’s latest recorded weight), `Frequency` (enum: BID, TID, SID, QID), `DurationDays` (int), `TotalQuantity` (decimal, computed).
- Compute `TotalQuantity = DosagePerKg * WeightKg * Frequency * DurationDays` on the server. Display it to the vet for confirmation. Reject if the computed total exceeds a configurable threshold (e.g., > 500 tablets — clearly a typo).
- Store `WeightKg` at the time of prescription (snapshot), not just a reference to the pet’s current weight. Pets gain/lose weight; a prescription written for a 12 kg dog that is now 20 kg could be under‑dosed.
- Provide a drug interaction checker (start with a local table of common vet drug pairs; extend to an API later). Block or warn on dangerous combinations.
- The prescription entity must be **immutable after dispensing** — edits create a new prescription record, preserving the audit trail.

**Warning signs:**
- Dosage is a free‑text field (e.g., “1 tab BID”).
- No server‑side calculation of total quantity.
- No weight snapshot on the prescription.
- Prescriptions can be edited after being marked “dispensed.”

**Phase to address:**
Phase 1 (prescription entity design) — dosage logic belongs in the domain model, not in Razor view JavaScript.

---

### Pitfall 4: Reminder System as a Cron Job That Emails Only

**What goes wrong:**
Vaccination and medication reminders are implemented as a single `Hangfire` or `BackgroundService` job that runs daily, queries all pets, and sends an email for every reminder due today. No SMS, no push, no in‑app notification. The owner never sees the email (it’s in spam). The vet has no visibility into which reminders were sent, which bounced, which were acknowledged. When the background job fails at 3 AM, nobody knows — pets miss vaccinations.

**Why it happens:**
“A reminder is just a scheduled email.” Developers pick the simplest path: a daily timer, an email send, call it done. But reminders are a **multi‑channel, multi‑stakeholder communication system**. The vet needs to know the owner got the reminder. The owner needs to see it in the app. The platform needs to know if it bounced.

**How to avoid:**
- Model `Reminder` as an entity with: `PetId`, `ReminderType` (Vaccination, Medication, Checkup, Lab), `Channel` (Email, SMS, InApp, Push), `ScheduledDate`, `Status` (Pending, Sent, Delivered, Acknowledged, Failed, Bounced), `SentAt`, `AcknowledgedAt`.
- The background job *creates* Reminder records; a separate *sender* service picks up `Pending` reminders and dispatches them via the appropriate channel. This separation allows retry, channel switching, and auditing.
- Provide an **in‑app notification center** (Razor partial + jQuery) that shows pending reminders on the owner’s dashboard. This is the primary channel; email/SMS are fallbacks.
- The vet dashboard shows a “Reminder Status” panel: how many reminders are pending, sent, failed. Failed reminders trigger an alert.
- Use the **Outbox pattern**: write the reminder to the Outbox table in the same transaction as the business event (e.g., vaccination administered → create reminder for next dose). Background worker sends from Outbox. This guarantees at‑least‑once delivery without duplicating.

**Warning signs:**
- Reminder is a single `DateTime` field on the pet entity.
- Only email channel, no in‑app or SMS.
- No status tracking (sent/delivered/acknowledged).
- Background job has no dead‑letter queue or retry policy.
- Owner has no way to see reminders without checking email.

**Phase to address:**
Phase 2 (reminder infrastructure) — the Outbox + multi‑channel architecture must be designed before any reminder UI is built. Retrofitting the Outbox after reminders are already firing via a cron job is a rewrite.

---

### Pitfall 5: Vaccination Schedule Hardcoded Instead of Configurable per Species/Breed

**What goes wrong:**
The vaccination reminder schedule is hardcoded: “Rabies every year, DHPP every 3 years.” But vaccination schedules vary by species (dog vs cat vs rabbit), breed (some breeds require more frequent heartworm testing), region (rabies laws differ by state/country), and vaccine manufacturer (some rabies vaccines are 3‑year, some are 1‑year). The system sends incorrect reminders. Vets override manually. The feature is abandoned.

**Why it happens:**
Hardcoding is fast for the demo. “We’ll make it configurable later.” Later never comes because the hardcoded version “works” for the most common case. But veterinary medicine is full of exceptions — and the exceptions are the ones that matter for patient safety.

**How to avoid:**
- Create a `VaccinationProtocol` entity: `Species`, `Breed` (nullable), `VaccineName`, `IntervalMonths`, `FirstDoseAgeMonths`, `BoostersRequired`, `Region` (nullable).
- The reminder creation logic reads from `VaccinationProtocol` to compute the next due date. The vet can override per‑patient (e.g., “this dog gets DHPP every 2 years due to titer testing”).
- Seed the table with AVMA’s core vaccine guidelines (AAHA Canine Vaccination Protocol, AAFP Feline Vaccination Protocol). Allow the vet clinic to add custom protocols.
- The UI shows a “Suggested Schedule” dropdown based on species/breed, pre‑populated from the protocol table. The vet confirms or overrides.

**Warning signs:**
- Vaccination schedule is in a switch statement or hardcoded intervals in the reminder job.
- No `VaccinationProtocol` entity in the domain model.
- “We’ll add a config page later” — config page is the foundation, not a later addition.

**Phase to address:**
Phase 1 (vaccination protocol entity) — the data model must support per‑species/breed schedules before any reminder logic is written.

---

### Pitfall 6: Medication Reminder Frequency Ignoring Pet’s Daily Routine

**What goes wrong:**
The system sends a medication reminder at 9 AM every day. But the pet’s medication is “with food, twice daily” — the owner feeds the pet at 7 AM and 6 PM. The 9 AM reminder is irrelevant and ignored. After three ignored reminders, the owner disables notifications entirely. The pet misses doses.

**Why it happens:**
Frequency is modeled as a simple interval (every 12 hours) without considering the pet’s feeding schedule, the owner’s routine, or the medication’s pharmacokinetic requirements. The reminder is technically correct but practically useless.

**How to avoid:**
- Model `MedicationSchedule` with: `MedicationName`, `Frequency` (enum: BID, TID, SID, QID, PRN), `PreferredTimes` (list of TimeOnly — e.g., ["07:00", "18:00"]), `WithFood` (bool), `FoodOffsetMinutes` (int — e.g., 30 minutes after feeding).
- The owner configures feeding times in the pet profile. The system computes reminder times relative to feeding.
- Provide a “snooze” or “reschedule” option on reminders — if the owner is running late, they can shift the reminder by 30 minutes, not just dismiss it.
- Track adherence: if a reminder is dismissed 3 times in a row without acknowledgment, send a “gentle nudge” or alert the vet.

**Warning signs:**
- Reminder time is hardcoded (e.g., always 9 AM).
- No concept of “with food” or feeding schedule.
- Reminder is a simple `DateTime` field with no preferred time configuration.
- Adherence tracking is absent — the system doesn’t know if the owner gave the medication.

**Phase to address:**
Phase 2 (medication reminder design) — the schedule model must be flexible from the start. Hardcoded times are a dead end.

---

### Pitfall 7: Vet Dashboard Without Role‑Based Data Segmentation

**What goes wrong:**
The vet dashboard shows **all** pets in the system. A vet at Clinic A sees Clinic B’s patients. A vet sees pets belonging to other vets in the same clinic (should only see their own patients unless they’re a supervisor). The admin sees the same view as the vet. There’s no concept of “my patients” vs “all patients in my clinic” vs “all patients in the system.”

**Why it happens:**
The initial implementation uses a simple `WHERE Role = 'Vet'` filter. Multi‑tenancy (clinic isolation) is handled by `TenantId`, but *within* a clinic, vets should only see their own patients unless they have a supervisor role. This intra‑tenant segmentation is often missed.

**How to avoid:**
- The vet dashboard must filter by `VetId = currentUser.Id` by default, with a toggle to “Show all patients in my clinic” (for supervisors/admins).
- The `Pet` entity already has a `TenantId` (clinic). Add a `PrimaryVetId` foreign key to `Pet` (nullable — not all pets have a primary vet yet).
- The vet dashboard query: `WHERE TenantId = currentTenant AND (PrimaryVetId = currentUser.Id OR currentUser.HasClaim("ClinicSupervisor"))`.
- The admin dashboard shows all pets in the tenant. The super‑admin (platform admin) shows all tenants.
- Write integration tests that verify Vet A cannot see Vet B’s patients within the same clinic (unless Vet A is a supervisor).

**Warning signs:**
- Vet dashboard loads all pets in the tenant with no vet‑level filter.
- No `PrimaryVetId` on the Pet entity.
- No distinction between “my patients” and “all clinic patients.”
- No supervisor role defined in the claims system.

**Phase to address:**
Phase 0 (vet dashboard foundation) — the data segmentation must be designed before any dashboard UI is built. Retrofitting `PrimaryVetId` after months of data is painful.

---

### Pitfall 8: Reminder Overload (Notification Fatigue)

**What goes wrong:**
The system sends a vaccination reminder, a medication reminder, a checkup reminder, and a “pet’s birthday” reminder — all on the same day. The owner receives 4 notifications, ignores all of them. After a week, they disable notifications entirely. Critical vaccination reminders are missed.

**Why it happens:**
Each reminder feature is built independently. No one designs a **notification budget** or **digest** system. The platform optimizes for “completeness” (never miss a reminder) but the owner experiences spam.

**How to avoid:**
- Implement a **notification digest**: group all reminders for a pet into a single daily or weekly summary (configurable by the owner).
- Provide **priority levels** on reminders: `Critical` (vaccination overdue, medication missed), `Normal` (upcoming checkup), `Informational` (pet birthday). Critical reminders are immediate; others are batched.
- The owner can configure quiet hours (no notifications between 10 PM and 7 AM) and preferred delivery time (e.g., “send digest at 8 AM”).
- Track notification engagement: if open rate drops below 30%, suggest the owner switch to weekly digest.

**Warning signs:**
- Owner receives more than 2 notifications per day on average.
- No digest option.
- No priority levels on reminders.
- No quiet hours configuration.

**Phase to address:**
Phase 2 (reminder UX design) — the digest and priority system must be designed alongside the reminder entity, not added as an afterthought.

---

### Pitfall 9: Prescription refill Tracking Falls Through the Cracks

**What goes wrong:**
A pet is prescribed a 30‑day medication. The system sends a reminder on day 30 to “refill prescription.” But the owner needs to refill on day 25 (to avoid running out). The system doesn’t track when the prescription was *dispensed* vs when it *expires*. The owner runs out of medication for 3 days. The vet gets an angry call.

**Why it happens:**
Refill tracking is modeled as “prescription end date = start date + duration.” But real‑world dispensing is messy: the pharmacy takes 2 days to deliver, the owner starts a day late, the vet authorizes an early refill. Without tracking actual dispensing and remaining supply, the reminders are wrong.

**How to avoid:**
- Model `PrescriptionRefill` with: `PrescriptionId`, `DispensedDate`, `QuantityDispensed`, `ExpectedEndDate` (computed from dispensing date + duration), `ActualEndDate` (when the owner marks it complete or the vet ends it).
- Send a refill reminder at `ExpectedEndDate - 5 days` (configurable lead time).
- Allow the owner to mark “I’ve refilled” which creates a new `PrescriptionRefill` record and resets the timer.
- The vet dashboard shows “prescriptions running low” (expected end date within 7 days) as a priority panel.

**Warning signs:**
- No `DispensedDate` on the prescription.
- Refill reminder is based on prescription start date, not dispensing date.
- No lead time configuration (e.g., “remind me 5 days before running out”).
- No way for the owner to confirm refill.

**Phase to address:**
Phase 1 (prescription entity) — the refill tracking fields must be part of the initial prescription model.

---

### Pitfall 10: Medical Record Export as Afterthought (Interoperability Failure)

**What goes wrong:**
The vet dashboard stores all SOAP notes, treatment plans, and prescriptions internally. When a pet is referred to a specialist, the referring vet has to print the records, scan them, and email PDFs. The specialist re‑enters the data into their system. There’s no structured export (FHIR, XML, or even a clean CSV). The platform is a data silo.

**Why it happens:**
Export feels like a “nice to have.” The team focuses on the UI and forgets that veterinary medicine is a network — pets move between vets, clinics, and shelters. Without export, the platform is a one‑way door: data goes in but doesn’t come out.

**How to avoid:**
- Design the medical record entities with export in mind from day one: every entity has a `CreatedDate`, `ModifiedDate`, `CreatedBy` (vet), and `TenantId`.
- Provide a **FHIR‑compatible export** (even if it’s just a subset: Patient, Encounter, Condition, MedicationRequest). Use the FHIR R4 JSON format — it’s the standard for health data exchange.
- At minimum, provide a **structured PDF** export that includes all SOAP notes, treatment plans, and prescriptions for a pet, formatted for clinical hand‑off.
- The export function must respect tenant boundaries — Vet A cannot export Vet B’s records.

**Warning signs:**
- No export function on the vet dashboard.
- Medical records are stored as blobs or unstructured text.
- No `CreatedBy` or `CreatedDate` on clinical entities.
- Export would require writing a new query from scratch.

**Phase to address:**
Phase 1 (medical record entity design) — audit fields and export‑friendly schema must be in the initial design. Adding them later means a migration and backfill.

---

## Technical Debt Patterns

Shortcuts that seem reasonable but create long‑term problems.

| Shortcut | Immediate Benefit | Long‑term Cost | When Acceptable |
|----------|-------------------|----------------|-----------------|
| Store SOAP notes as a single `string` column | Fast to implement, simple CRUD | Impossible to query by diagnosis, vitals, or treatment; export is garbage; analytics impossible | **Never** for a clinical system — the whole point of a vet dashboard is structured clinical data |
| Dosage calculation in Razor/JS only | Fast to demo, no server‑side complexity | Lethal typos pass through; audit trail shows unvalidated dosages; no server‑side drug interaction check | **Never** — dosage math must be server‑side with validation |
| Reminder as a `DateTime` field on Pet | Simple, no extra tables | Only one reminder per pet; no multi‑channel; no status tracking; no digest | **Never** — reminders are a first‑class entity |
| Hardcoded vaccination schedule | Demo works in 10 minutes | Incorrect reminders for non‑standard protocols; vet overrides; feature abandoned | Only for a throwaway prototype — not for a clinical system |
| Treatment plan as a PDF blob | Looks like a “document” | Not searchable, not linkable, not auditable, not updatable; the plan is a workflow, not a file | **Never** — treatment plans are entities with relationships |
| Skip `AsNoTracking()` on vet dashboard queries | “It works fine” | EF Core tracking memory grows with every patient loaded; dashboard slows as patient count grows | **Never** — all read‑only dashboard queries must use `AsNoTracking()` |
| No adherence tracking on medication reminders | “The reminder is enough” | Owner stops acknowledging; vet has no visibility into compliance; medication effectiveness is unknown | Acceptable for MVP *if* the vet dashboard explicitly shows “adherence tracking: coming soon” — but plan it from day one |
| Single `Reminder` table with a `Type` column | Simple schema | All reminder types share the same fields; vaccination reminders need `ProtocolId`, medication reminders need `PrescriptionId` — the table becomes a god entity with nullable columns | Acceptable for MVP with 2–3 reminder types; split into separate entities when a fourth type is added |

---

## Integration Gotchas

Common mistakes when connecting to external services.

| Integration | Common Mistake | Correct Approach |
|-------------|----------------|------------------|
| Email provider (SendGrid, SMTP) | Sending from the request path; no retry on transient failures | Queue emails via Outbox; background worker sends with exponential backoff; dead‑letter after 3 retries |
| SMS provider (Twilio, Vonage) | SMS sent synchronously; no delivery status webhook | Queue SMS like email; register Twilio status callback to update `Reminder.Status` to `Delivered` or `Bounced` |
| Drug database API (e.g., veterinary drug formulary) | API called on every prescription save; no local cache | Cache drug data locally; sync nightly via background job; offline fallback with local table |
| FHIR export endpoint | Building a full FHIR server for v1 | Provide FHIR *export* only (Patient, Encounter, Condition, MedicationRequest JSON); don’t build a FHIR server |
| Notification push (Firebase, OneSignal) | Push notifications require mobile app (out of scope) | Defer push notifications; focus on in‑app + email + SMS for v1 |
| Calendar integration (Google Calendar, Outlook) | Building a full calendar sync in v1 | Provide an `.ics` file download for appointments; full calendar sync is a future phase |

---

## Performance Traps

Patterns that work at small scale but fail as usage grows.

| Trap | Symptoms | Prevention | When It Breaks |
|------|----------|------------|----------------|
| Vet dashboard loads all patients without pagination | Page load time grows linearly with patient count; 500ms at 100 patients, 5s at 1,000 | Mandatory pagination (page size 20, max 100) on patient list; `AsNoTracking()` on all dashboard queries | ~500 patients |
| Eager‑loading entire pet graph for SOAP note view | `.Include(p => p.MedicalRecords).ThenInclude(m => m.SoapNotes).ThenInclude(s => s.Prescriptions)` — 10+ joins | Use separate queries for each section (vitals, SOAP, prescriptions) or projection with `.Select()` | ~50 medical records per pet |
| Reminder job queries all pets daily without index | Full table scan on `Pet` table; SQL Server CPU spikes at 3 AM when job runs | Index on `Reminder.ScheduledDate` and `Reminder.Status`; query only `Pending` reminders with `WHERE ScheduledDate <= Today` | ~10K pets |
| Medication reminder deduplication via application logic | Every reminder check loads all pending reminders into memory, deduplicates in C# | Use a SQL `GROUP BY` with `MIN(ScheduledDate)` to deduplicate at the database level | ~5K active medication reminders |
| SOAP note search without full‑text index | `WHERE Objective LIKE '%ear infection%'` — full table scan on every search | Enable SQL Server full‑text search on SOAP note text fields; or use a lightweight search library (Lucene.NET) | ~1K SOAP notes |
| Dashboard aggregates computed on every page load | “Pets seen today” query runs every time the vet dashboard loads | Cache aggregates in a `DashboardSnapshot` table; refresh every 5 minutes via background job | ~100 dashboard loads/minute |

---

## Security Mistakes

Domain‑specific security issues beyond general web security.

| Mistake | Risk | Prevention |
|---------|------|------------|
| Vet can see SOAP notes for pets they didn’t examine | Privacy violation; vet sees another vet’s clinical judgment | Enforce `CreatedByVetId` on SOAP notes; dashboard shows only notes created by the current vet (unless supervisor role) |
| Prescription data accessible without pet ownership verification | Owner A sees Owner B’s pet’s prescriptions | Every medical record endpoint must verify: `pet.TenantId == currentTenant AND (pet.OwnerId == currentUser.Id OR currentUser.HasClaim("Vet"))` |
| Reminder notifications contain sensitive medical info in email | Email is unencrypted; contains pet diagnosis and medication details | Email reminders contain only “You have a reminder for [PetName] — log in to view details.” Never include diagnosis or drug names in email body |
| Treatment plan export accessible without authorization |任何人都 can export a pet’s full medical history | Export endpoint requires `Authorize` + resource‑based authorization (owner or treating vet) |
| SOAP notes editable after signed/completed | Vet’s clinical notes are altered after the fact; legal liability | Mark SOAP notes as `Signed` (immutable) after the vet clicks “Complete.” Edits create a new version, not overwrite the old one |
| Background reminder job runs with elevated privileges | Job can access all tenants; a bug in the job leaks data across tenants | Background jobs must restore tenant context from the Outbox message payload; never run with a blanket `Admin` claim |
| Drug interaction check uses a third‑party API without caching | API downtime means no safety check; or API response is slow, blocking prescription save | Cache drug interaction data locally; sync nightly; block prescription only if local data flags a *known* dangerous interaction; warn (don’t block) for unknown interactions |

---

## UX Pitfalls

Common user experience mistakes in this domain.

| Pitfall | User Impact | Better Approach |
|---------|-------------|-----------------|
| SOAP note form requires filling all four sections before saving | Vet abandons the note mid‑exam; data lost | Allow saving as draft at any point; auto‑save every 30 seconds; show “Draft” badge on incomplete notes |
| Treatment plan is a separate page from the SOAP note | Vet has to navigate away from the patient’s record to create a plan; loses context | Treatment plan creation is a tab or section within the SOAP note view; the plan is created *from* the SOAP note, not separately |
| Medication reminder acknowledgment is a single “OK” button | Vet has no insight into whether the owner actually gave the medication | Acknowledgment requires confirmation: “Did you give the medication? [Yes] [No — reschedule] [No — contact vet]” |
| Vaccination reminder shows only the vaccine name | Owner doesn’t know why the vaccine is important or what happens if they skip it | Include a brief explanation: “Rabies vaccine is required by law. Overdue by 3 days. [Schedule now] [I already did — update record]” |
| Vet dashboard is a single long scrollable page | Vets with 200+ patients scroll endlessly; no quick access to urgent cases | Dashboard has tabs: “My Patients” (paginated list), “Today’s Appointments” (time‑based), “Urgent” (overdue vaccinations, missed medications), “Recent Notes” (last 10 SOAP notes) |
| Prescription form doesn’t show pet’s weight | Vet has to remember or look up the weight; dosage calculation is manual | Auto‑populate pet’s latest recorded weight at the top of the prescription form; highlight if weight is > 30 days old (“Weight may be outdated — recheck before prescribing”) |
| Reminder preferences are buried in account settings | Owner never configures reminders; gets default (email only, 9 AM) | First‑time reminder setup wizard: “How do you want to be reminded? [Email] [SMS] [In‑app] [All]. When? [Morning] [Afternoon] [Evening]. Quiet hours? [10 PM – 7 AM]” |
| Export button on vet dashboard exports *all* records for the clinic | Vet accidentally exports another vet’s patients; data leak | Export is per‑pet, not per‑clinic. The export button is on the pet detail page, not the dashboard. Confirmation dialog: “Export records for [PetName]? This will include all SOAP notes, prescriptions, and treatment plans.” |

---

## "Looks Done But Isn't" Checklist

Things that appear complete but are missing critical pieces.

- [ ] **SOAP notes:** Often missing structured vitals (weight, temp, HR), diagnosis coding, and draft/auto‑save — verify all three exist per SOAP note.
- [ ] **Treatment plans:** Often missing link to originating SOAP note, linked prescriptions, status (Active/Completed), and audit trail (who created, when) — verify all four.
- [ ] **Prescriptions:** Often missing server‑side dosage calculation, weight snapshot, drug interaction check, immutability after dispensing, and refill tracking — verify all five.
- [ ] **Vaccination reminders:** Often missing per‑species/breed protocol, configurable schedule, multi‑channel delivery, and acknowledgment tracking — verify all four.
- [ ] **Medication reminders:** Often missing “with food” scheduling, preferred times, adherence tracking, and snooze/reschedule option — verify all four.
- [ ] **Vet dashboard:** Often missing vet‑level patient filtering, urgent‑case highlighting, pagination, and `AsNoTracking()` — verify all four.
- [ ] **Reminder system:** Often missing Outbox pattern, dead‑letter queue, delivery status tracking, and notification digest — verify all four.
- [ ] **Medical record export:** Often missing FHIR‑compatible format, audit fields (CreatedBy, CreatedDate), and tenant‑scoped access — verify all three.
- [ ] **Notification preferences:** Often missing quiet hours, channel selection, priority levels, and digest configuration — verify all four.
- [ ] **Prescription refill:** Often missing dispensing date, expected end date, lead‑time configuration, and owner refill confirmation — verify all four.

---

## Recovery Strategies

When pitfalls occur despite prevention, how to recover.

| Pitfall | Recovery Cost | Recovery Steps |
|---------|---------------|----------------|
| SOAP notes stored as unstructured text for 6 months | HIGH — data migration, possible data loss | 1. Write a migration script to parse existing free‑text SOAP notes (regex for vitals, diagnosis keywords). 2. Create structured entities. 3. Backfill from parsed data (accept that some data will be imperfect). 4. Deploy new form; keep old form as read‑only archive. |
| Dosage calculations done only in JS, lethal typo in production | CRITICAL — patient safety incident | 1. Immediately add server‑side dosage validation. 2. Audit all existing prescriptions for dosage anomalies (> 10× normal range). 3. Notify affected vets. 4. Add drug interaction checker. |
| Reminder cron job fails silently for 2 weeks | HIGH — missed vaccinations, owner trust damage | 1. Add monitoring/alerting on the background job (email admin on failure). 2. Implement dead‑letter queue. 3. Send a catch‑up batch of overdue reminders with “Sorry for the delay” messaging. |
| Vet can see other vet’s patients (no intra‑tenant filter) | MEDIUM — privacy violation, trust damage | 1. Add `PrimaryVetId` to Pet entity (migration). 2. Backfill from `CreatedBy` on medical records. 3. Add vet‑level filter to dashboard query. 4. Add integration test. |
| Vaccination schedule hardcoded, sends wrong reminders | MEDIUM — clinical risk, vet frustration | 1. Create `VaccinationProtocol` entity. 2. Seed with AAHA/AAFP guidelines. 3. Migrate existing reminders to use protocol. 4. Add vet override capability. |
| Treatment plans stored as PDF blobs | HIGH — data silo, no search, no audit | 1. Create `TreatmentPlan` entity with relationships. 2. Write a parser to extract text from existing PDFs (if possible). 3. Backfill entity from parsed data. 4. Deprecate PDF upload. |

---

## Pitfall‑to‑Phase Mapping

How roadmap phases should address these pitfalls.

| Pitfall | Prevention Phase | Verification |
|---------|------------------|--------------|
| SOAP notes as unstructured text | Phase 0 (domain model) | `SoapNote` entity has structured vitals, diagnosis, and draft support |
| Treatment plans as static documents | Phase 1 (treatment plan entity) | `TreatmentPlan` links to `SoapNote`, `Pet`, and `Prescription` |
| Dosage calculations in UI only | Phase 1 (prescription entity) | Server‑side dosage validation with weight snapshot |
| Reminder as cron email only | Phase 2 (reminder infrastructure) | Outbox pattern, multi‑channel, status tracking |
| Hardcoded vaccination schedule | Phase 1 (vaccination protocol) | `VaccinationProtocol` entity with per‑species/breed data |
| Medication reminder ignores feeding schedule | Phase 2 (reminder UX) | `MedicationSchedule` entity with `PreferredTimes` and `WithFood` |
| Vet dashboard shows all patients | Phase 0 (vet dashboard) | Integration test: Vet A cannot see Vet B’s patients |
| Reminder overload / notification fatigue | Phase 2 (reminder UX) | Notification digest option, priority levels, quiet hours |
| Prescription refill tracking gaps | Phase 1 (prescription entity) | `DispensedDate`, `ExpectedEndDate`, lead‑time config |
| Medical record export as afterthought | Phase 1 (medical record design) | FHIR export endpoint, audit fields on all clinical entities |
| Prescription editable after dispensing | Phase 1 (prescription entity) | Immutability enforcement on `Dispensed` status |
| Background job without tenant context | Phase 2 (reminder infra) | Integration test: reminder job respects tenant boundaries |
| SOAP notes editable after signing | Phase 0 (domain model) | `Signed` status prevents edits; versioning on update |
| Export requires no authorization | Phase 1 (export feature) | Resource‑based authorization on export endpoint |

---

## Sources

- **AVMA Medical Records Guidelines:** avma.org — veterinary medical record standards, SOAP note structure
- **AAHA Canine Vaccination Protocol:** aaha.org — core vaccine schedules, booster intervals
- **AAFP Feline Vaccination Protocol:** aafp.org — feline‑specific vaccine guidelines
- **FHIR R4 Specification:** hl7.org/fhir — healthcare data exchange standard
- **ASP.NET Core Security Documentation:** learn.microsoft.com — authorization handlers, resource‑based auth, data protection
- **Veterinary Software Post‑mortems:** VIN News, DVM360 — common EHR implementation failures in vet clinics
- **Notification Fatigue Research:** Nielsen Norman Group — digest vs. real‑time notification UX patterns
- **Drug Interaction Checking in Vet Medicine:** Veterinary Information Network — common drug pairs, interaction severity

---
*Pitfalls research for: Vet Dashboard & Reminders — ASP.NET Core MVC Pet Platform*
*Researched: 2026-07‑21*