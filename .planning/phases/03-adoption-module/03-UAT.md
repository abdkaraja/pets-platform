---
status: testing
phase: 03-adoption-module
source: [03-01-SUMMARY.md, 03-02-SUMMARY.md, 03-03-SUMMARY.md]
started: 2026-07-21T18:00:00Z
updated: 2026-07-21T18:00:00Z
---

## Current Test

number: 1
name: Public Adoption Browse Page
expected: |
  Navigate to /Adoption shows a search page with filter sidebar (Species, Breed, Age Range, Location, Search input).
  Adoption listings display in a card grid with pet photo, name, species, breed, age, location, and status badge.
  Pagination works correctly.
  No authentication required.
awaiting: user response

## Tests

### 1. Public Adoption Browse Page
expected: Navigate to /Adoption shows a search page with filter sidebar (Species, Breed, Age Range, Location, Search input). Adoption listings display in a card grid with pet photo, name, species, breed, age, location, and status badge. Pagination works correctly. No authentication required.
result: passed

### 2. Adoption Listing Details Page
expected: Click an adoption listing card to view full details. Page shows pet photo gallery, full description, species, breed, age, location, shelter info, and listing status. If logged in and listing is Active, an "Apply" button is displayed. If not logged in, no Apply button shown.
result: [pending]

### 3. Customer - Submit Adoption Application
expected: Log in as Customer user. Navigate to an Active adoption listing. Click "Apply" button. Application form shows listing summary, Message field, household info (HousingType dropdown, HasYard checkbox, NumberOfOccupants, HasChildren), and pet experience fields (PreviousPets, CurrentPets, ExperienceLevel). Submit form. Redirect to My Applications page with new application showing "Submitted" status.
result: [pending]

### 4. Customer - View My Applications
expected: Log in as Customer. Navigate to /Customer/Adoption/MyApplications. Table shows all user's applications with listing name, status badge (color-coded), date submitted, and conditional Withdraw button (only for Submitted applications). Click Withdraw on a Submitted application. Confirm dialog. Status changes to Withdrawn.
result: [pending]

### 5. Customer - View Application Details
expected: Log in as Customer. Click on an application in My Applications. ApplicationDetails page shows full application info, listing details, status history timeline, and review notes (if reviewed). Status history shows chronological status changes with timestamps.
result: [pending]

### 6. Shelter - Manage Listings Dashboard
expected: Log in as Shelter/ServiceProvider user with Adoptions.Manage permission. Navigate to ServiceProvider/Adoption/Listings. Dashboard shows all shelter's listings with status badges (Active=green, Pending=yellow, Adopted=blue, Closed=gray), application counts, and conditional Close button (only for Active listings). Click Close on an Active listing. Confirm dialog. Status changes to Closed.
result: [pending]

### 7. Shelter - Create New Listing
expected: Log in as Shelter user. Navigate to Create Listing form. Form shows Pet dropdown (if linked to existing pet), Title, Description, Species, Breed, Age, Location, AdoptionFee, and Status fields. Submit form. Redirect to Listings dashboard with new listing appearing in the list.
result: [pending]

### 8. Shelter - Edit Listing
expected: Log in as Shelter user. Click Edit on an existing listing. Edit form pre-fills with current values. Modify Description and AdoptionFee. Submit form. Redirect to Listings dashboard with updated values showing.
result: [pending]

### 9. Shelter - Review Application
expected: Log in as Shelter user. Navigate to Applications for a listing. See list of applications with applicant info, status, and submission date. Click Review on a Submitted/UnderReview application. Review form shows full application context (household info, pet experience, message). Select Approve or Reject radio button. Add ReviewNotes. Submit. Application status updates. Redirect to Applications list.
result: [pending]

### 10. Authorization - Protected Routes
expected: Without login, navigating to /Customer/Adoption/* redirects to login page. Without Adoptions.Manage permission, navigating to /ServiceProvider/Adoption/* returns Forbidden. Customer cannot access Shelter management pages. Shelter cannot submit applications as Customer.
result: [pending]
