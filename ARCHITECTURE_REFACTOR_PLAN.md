# AryamanBMS Architecture Refactor Plan

## Accelerated Layering Milestone — Completed

The application now has clear physical project ownership without changing its
runtime behaviour:

- `AryamanBMS.Models` owns 95 entity files.
- `AryamanBMS.Database` owns 13 DbContexts, including the central
  `ApplicationDbContext`.
- The central `ApplicationDbContext` retains its `AryamanBMS.Data` namespace,
  74 remaining DbSets, existing EF mappings, and migrations. No schema change
  was introduced by this move.
- `AryamanBMS.Repositories` owns 57 repository interfaces and 57 repository
  implementations.
- Controllers, Razor views, view models, and Web-only services remain in the
  Web project by design.

The completed ownership move passed the architecture dependency guard, Debug
and Release builds, and representative runtime smoke tests. Future work may
split the central context by complete transactional aggregate; it must not
separate records that need to be saved atomically.

## Branch

`refactor/complete-project-layering`

## Baseline

- Date: 22-Aug-2026
- Build: successful
- Command:
  `dotnet build AryamanBMS/AryamanBMS.csproj --no-restore`

## Non-Negotiable Safety Rules

1. No database schema changes unless explicitly approved.
2. No route, view, JavaScript, CSS, or user-workflow changes.
3. Refactor one module at a time.
4. Build after every completed micro-step.
5. Manually test the affected module before the next module.
6. Commit every green module checkpoint.
7. Do not delete old code until its replacement compiles and is tested.

## Target Project Ownership

| Project                 | Owns                                                  |
| ----------------------- | ----------------------------------------------------- |
| AryamanBMS              | Controllers, Views, ViewModels, UI, DI composition    |
| AryamanBMS.Business     | Business workflows, domain services, validation rules |
| AryamanBMS.Repositories | Repository interfaces and data-access implementations |
| AryamanBMS.Database     | EF DbContexts, configurations, migrations, seeds      |
| AryamanBMS.Models       | Domain entities, enums, contracts                     |
| AryamanBMS.Utilities    | Pure helpers, constants, extensions                   |

## Dependency Direction

`AryamanBMS → Business → Repositories → Database → Models`

`Utilities` may be used by every project but must not depend on any project.

## First Pilot Module

Location and Country.

## Complete Module Inventory

| Module                     | Main current responsibilities                                     | Planned refactor order |
| -------------------------- | ----------------------------------------------------------------- | ---------------------: |
| Shared foundation          | Utilities, constants, shared validation, file-storage contracts   |                      1 |
| Master data                | Country, Location, Department, Designation, Company Profile       |                      2 |
| Platform & administration  | Account, User, Role, System, Login History, Notifications         |                      3 |
| HR & attendance            | Employee, Attendance, Leave, Holiday, Working Days, Comp-off      |                      4 |
| Projects & work management | Projects, Members, Tasks, Timeline, Flow, Meetings, Risks         |                      5 |
| CRM & proposals            | Clients, Communications, Proposals, Proposal Templates            |                      6 |
| Finance & billing          | Invoices, Receipts, Notes, Receivables, Vendors, Expenses, Assets |                      7 |
| Compliance                 | GST, LUT, PF, ESIC, PT, Financial Audit Documents                 |                      8 |
| Documents & reporting      | Secure documents, letters, PDF/DOCX generation, reports           |                      9 |

## Current-State Finding

The remaining central EF ownership is now in `AryamanBMS.Database`:
`ApplicationDbContext`, its 74 remaining DbSets, and 95 entity files are no
longer physically owned by the Web project. Controllers, views, view models,
and Web-only services remain in the Web project by design.

The refactor moves responsibility without altering business behaviour.

## Migration Definition of Done

A module is complete only when:

1. Its code is in the correct target project.
2. The Web controller contains only HTTP/UI orchestration.
3. The solution builds successfully.
4. Existing user workflow checks pass.
5. The module checkpoint is committed and pushed.

## Completed Refactor Slices

- Location and Country: location entities, DbContext, and repository layering.
- Company Documents: category and document entities, context, repositories, registrations, and file-workflow consumers layered as one aggregate.
- Financial Constants and Financial Year: constants moved to Models; service moved to Business.
- Attendance Calendar and Working Days: calendar entities, context, repository, options, and service layering.
- Password Change Logs: entity, context, repository, service, and controller consumers layered.
- Company Profile: entity, context, repository, registration, and document-service consumer layered.
- GST Configuration: entity, context, repository, registration, and invoice consumer layered.
- GST LUT Documents: entity, GST context mapping, repository, and controller document flow layered.
- Financial Audit Documents: entity, context, repository, and registration layered.
- Payroll Configuration: payroll policy, payroll period lock, and professional-tax slabs moved to Models and owned by `PayrollConfigurationDbContext`.
- Login History: entity, context, repository, service, and controller consumers layered.
- Notifications: entity, context, repository, and controller data access layered; the SignalR notification adapter remains intentionally in Web.
- Calendar Manual Events: entity, context, repository, controller, and calendar-service consumers layered.
- Holidays: entity ownership moved to the attendance-calendar context; controller, Excel import, and calendar consumers use `IHolidayRepository`.
- Working Day Overrides: repository-backed controller flow completed; ownership is solely in `AttendanceCalendarDbContext`.
- Notices: `NoticeModel` and `NoticeDocumentModel` moved together with their parent/document relationship, repository, and `NoticeDbContext`.
- User-facing calendar register improvements: authenticated Holiday register and Birthday register added, with calendar links corrected and verified.
- Architecture guard: `scripts/Test-Architecture.ps1` validates project-reference direction locally and in GitHub Actions.
- Accelerated ownership move: all remaining entity files were moved to AryamanBMS.Models`; the central `ApplicationDbContext` was moved to `AryamanBMS.Database` without namespace, mapping, or migration changes; and all compatible repository contracts and implementations were moved to `AryamanBMS.Repositories`.
- Expense Category: list filtering/sorting and create, edit, duplicate-code,
  GST-rate validation, and soft-delete rules now flow through a Business
  service backed by the existing repository; the workflow was manually
  verified and checkpointed.

## Current Layering Inventory

As of 25-Aug-2026:

- `AryamanBMS.Models`: 95 entity files
- `AryamanBMS.Database`: 13 DbContexts, including the central `ApplicationDbContext`
- `AryamanBMS.Repositories`: 57 repository interfaces and 57 implementations
- Central `ApplicationDbContext`: 74 DbSets retained for future aggregate-based context splitting

## Remaining Central ApplicationDbContext Aggregates

Location entities remain mapped by the central `ApplicationDbContext` as shared
references for their parent aggregates. Their entity ownership is already in
Models; a later context split can remove the central mappings when the parent
aggregates move.

| Aggregate | Remaining ownership |
| --- | --- |
| Master data and employees | Country, State, City, Pincode, Department, Designation, Employee, employee academic/document/salary records |
| Attendance and leave | Attendance, Leave Type, Leave Balance, Leave Application/Days, Comp-off, Off-day Work Requests |
| Payroll processing | Salary Import Batch, Salary Record, Salary Payment Batch, Salary Advance, Full and Final Settlement, Financial Sequence |
| Projects | Project, members, tasks, progress, flow, timeline, communications, meetings, risks, scope documents |
| CRM and proposals | Client, client communication, Proposal, proposal audit/document versions, Purchase Orders |
| Finance and billing | Invoice/details/document versions, receipts, credit/debit notes, vendor/payments, expense vouchers/documents, billing milestones, advance receipts |
| GST compliance | GST snapshots, returns, challans, ITC records, and documents |
| PF, ESIC, and PT compliance | Monthly snapshots, challans, and documents for each compliance stream |
| Office assets and letters | Office assets with assignment/history/documents/maintenance/verification, Letters |

Location entities remain mapped by `ApplicationDbContext` as transitional
shared references for their still-legacy parent aggregates. Their completed
moves are therefore retained, but their old mappings cannot be removed until
those parent aggregates move.

## Verification

- Project-reference direction audited: no lower-layer project references the Web project.
- The reference-direction guard runs locally and in GitHub Actions.
- No migrations or database schema changes were introduced.
- Each completed slice was built with `dotnet build AryamanBMS.slnx --no-restore`.
- Affected workflows were manually verified during each slice.
