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

## Remaining Work

Refactor the remaining controller workflows into Business services, in this
order:

1. Finance: Invoice, Credit/Debit Note, Advance Receipt, Billing Milestone,
   Vendor Payment, Office Asset.
2. CRM: Client, Client Communication, Proposal, Proposal Template,
   Purchase Order.
3. Projects: Project, members, tasks, timeline, flow, meetings, risks,
   communications.
4. HR and payroll: Employee, Attendance, Leave, Comp-off, Salary, advances,
   payments, and full-and-final settlement.
5. Compliance and platform: GST operational flows, PF, ESIC, PT, Account,
   User, Dashboard, Letters.

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
