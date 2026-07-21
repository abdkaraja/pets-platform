---
status: passed
phase: 04-lost-pets
source: 04-01-SUMMARY.md, 04-02-SUMMARY.md, 04-03-SUMMARY.md
started: "2026-07-21T00:00:00.000Z"
updated: "2026-07-21T00:00:00.000Z"
---

## Current Test

number: 18
name: Report Details - Reporter Actions
expected: |
  When viewing own report (reporter), Edit and Resolve buttons shown (if Open). When viewing other user's report, no Edit/Resolve buttons shown. When not logged in, no action buttons shown.
awaiting: none (completed)

## Tests

### 1. Public Search Page Loads
expected: Navigating to /LostPet shows a search page with filter sidebar (Report Type dropdown, Species dropdown, Breed/Color/Location inputs, Date Range inputs, Search input) and a report card grid. No authentication required.
result: passed

### 2. Search Filters Work
expected: Applying Report Type filter (Lost/Found) shows only matching reports. Applying Species filter shows only matching species. Applying Location filter shows reports containing the search term. Filters combine correctly.
result: passed

### 3. Report Cards Display Correctly
expected: Each report card shows: Report Type badge (red for Lost, green for Found), first photo thumbnail (or placeholder), Species, Breed (if any), Color, Location, Date Reported, Description truncated to 150 chars. Cards link to Details page.
result: passed

### 4. Pagination Works
expected: When there are more than 12 reports, pagination appears at bottom. Previous/Next links work. Current page is highlighted.
result: skipped

### 5. Report Details Page Shows Full Info
expected: Clicking a report card navigates to Details page showing: Report Type badge (large, color-coded), Status badge (Open=blue, Resolved=gray), Photo gallery (responsive grid), Species/Breed/Color, Location (prominent), Date Reported, Full Description, Linked Pet section (if PetId set), Reporter info.
result: passed

### 6. Create Report Form - Auth Required
expected: Navigating to /Customer/LostPet/Create without login redirects to login page. After login, Create form loads with: ReportType radio buttons (Lost/Found), Species dropdown, Breed text input, Color text input, Location text input, DateReported date input (default today), Description textarea, PetId optional input, Photo upload section (1-5 files with JS validation).
result: passed

### 7. Create Report - Photo Validation
expected: Submitting with 0 photos shows validation error. Selecting more than 5 photos in JS shows alert and resets. Photo preview thumbnails appear when files selected. Photo count shows "X/5 photos selected".
result: passed

### 8. Create Report - Success Flow
expected: Submitting a valid report with 1-5 photos redirects to ReportDetails page with success message "Your lost pet report has been submitted! We're checking for matches...". Report appears in MyReports list.
result: passed

### 9. My Reports List
expected: Navigating to /Customer/LostPet/MyReports shows all user's reports in a table with columns: Type (badge), Species, Color, Location, Date, Status (badge), Actions. Edit link only shown for Open reports. Resolve button only shown for Open reports.
result: passed

### 10. Edit Report Form
expected: Clicking Edit on an Open report loads edit form pre-filled with existing values. ReportType, Species, PetId are read-only (shown as text). Existing photos displayed. New photo upload available. Total photo count enforced.
result: passed

### 11. Edit Report - Cannot Edit Resolved
expected: Edit button is not shown for Resolved reports. Attempting to navigate to Edit for a Resolved report redirects with error message.
result: passed

### 12. Resolve Report
expected: Clicking "Mark as Resolved" on an Open report shows confirmation dialog. After confirming, status changes to Resolved. Success message displayed. Edit button disappears.
result: passed

### 13. Notifications Page - Auth Required
expected: Navigating to /Customer/LostPet/Notifications without login redirects to login page. After login, notifications page loads showing match notifications.
result: passed

### 14. Notifications Display
expected: Each notification card shows: message describing the match, matched report summary (species, location, description), timestamp, "View Report" link, "Dismiss" button (only for unread). Unread notifications have blue left border. Read notifications have gray border.
result: skipped

### 15. Mark Notification Read
expected: Clicking "Dismiss" on an unread notification marks it as read. Visual state changes (blue border → gray). Dismiss button disappears.
result: skipped

### 16. Navigation Links
expected: "Lost Pets" link visible in main navigation for all users (logged in or not). "My Reports" and "Notifications" links visible only when logged in. All links navigate to correct pages.
result: passed

### 17. Matching Logic - Bidirectional Notifications
expected: Creating a Lost report for "Dog" in "Maadi", then creating a Found report for "Dog" in "Maadi" (different user) generates match notifications for BOTH users. Both reporters see notifications in their Notifications page.
result: skipped

### 18. Report Details - Reporter Actions
expected: When viewing own report (reporter), Edit and Resolve buttons shown (if Open). When viewing other user's report, no Edit/Resolve buttons shown. When not logged in, no action buttons shown.
result: skipped

## Summary

total: 18
passed: 12
issues: 0
pending: 0
skipped: 6
blocked: 0

## Gaps

[none yet]
