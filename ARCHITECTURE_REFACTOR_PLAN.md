# AryamanBMS Architecture Refactor Plan

## Goal

Keep the MVC application working exactly as it does today while making the
layers clear:

`Web -> Business -> Repositories -> Database -> Models`

`Utilities` may be used anywhere but must not depend on another project.

## What stays in Web

Controllers, views, view models, routes, authentication/authorization,
SignalR, file HTTP handling, Excel/PDF response formatting, JavaScript, CSS,
and dependency-injection setup.

## What moves out of Web

Business rules, calculations, validation, workflow/status transitions, EF Core
queries, persistence, and reusable contracts.

## Safety Rules

1. No schema or migration changes without approval.
2. No route, view, CSS, JavaScript, role, or workflow changes unless requested.
3. Refactor one bounded workflow at a time.
4. Build and run the architecture guard after each slice.
5. Manually verify the affected workflow before removing legacy code.
6. Commit and push every verified checkpoint.

## Current Status

### Structural layering — complete

- Models owns domain entities and shared contracts.
- Database owns DbContexts, mappings, migrations, and seeds.
- Repositories owns repository contracts and implementations.
- The architecture dependency guard runs locally and in GitHub Actions.

### Verified workflow slices — complete

- Location and Country
- Company Documents
- Financial Year and financial constants
- Attendance calendar, holidays, and working-day overrides
- Password change logs, login history, notifications, and calendar events
- Company Profile, GST configuration, GST LUT, and Financial Audit Documents
- Payroll configuration
- Notices
- Vendor
- Purchase Report, Receivables Report, and Accounts Finance dashboard
- Payment Receipt tracker
- Expense Category
- Expense Voucher: read/export selection, create, edit, transitions,
  documents, and repository-backed lookups
- Credit Note: list/query flow, transactional creation, and original-invoice
  balance/payment-status adjustment
- Debit Note: list/query flow, transactional creation, and original-invoice
  balance/payment-status adjustment
- Invoice: tracker queries, draft create/edit validation and persistence,
  issue/cancel/delete transitions, and document lookups
- Advance Receipt: tracker queries, receipt creation, and transactional
  application to issued invoices

## Remaining Work

Refactor these controller workflows into Business services in the listed order.

| Priority | Module | Remaining subtopics | Count |
| ---: | --- | --- | ---: |
| 1 | Finance | Billing Milestone; Vendor Payment; Office Asset | 3 |
| 2 | CRM | Client; Client Communication; Proposal; Proposal Template; Purchase Order | 5 |
| 3 | Projects | Project; Project Members; Project Tasks; Timeline; Flow; Meetings; Risks; Communications | 8 |
| 4 | HR and payroll | Employee; Attendance; Leave; Comp-off; Salary; Salary Advance; Salary Payment; Full and Final Settlement | 8 |
| 5 | Compliance and platform | GST operations; PF; ESIC; PT; Account; User; Dashboard; Letters | 8 |

**Total remaining workflow subtopics: 32.**

**Next workflow: Billing Milestone.**

## Deferred Cross-Cutting Work

| Area | What remains in Web | Why it is deferred |
| --- | --- | --- |
| Invoice notifications | Recipient selection and best-effort issue/cancel notification calls | `INotificationService` is currently a Web-owned contract. Move that shared contract to a lower layer before Business orchestrates notifications; this avoids reversing the dependency direction during the Invoice slice. |

The central `ApplicationDbContext` can be split later only by complete
transactional aggregate. It is not a bulk refactor task.

## Definition of Done for Each Workflow

- Controller contains HTTP/UI orchestration only.
- Business owns workflow rules and validation.
- Repository owns persistence.
- Build passes.
- Architecture guard passes.
- Workflow is manually verified.
- Checkpoint is committed and pushed.
