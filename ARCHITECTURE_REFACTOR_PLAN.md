# AryamanBMS Architecture Refactor Plan

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

| Project                 | Owns                                                       |
| ----------------------- | ---------------------------------------------------------- |
| AryamanBMS              | Controllers, Views, ViewModels, UI, DI composition         |
| AryamanBMS.Business     | Business workflows, domain services, validation rules      |
| AryamanBMS.Repositories | Repository interfaces and data-access implementations      |
| AryamanBMS.Database     | ApplicationDbContext, EF configurations, migrations, seeds |
| AryamanBMS.Models       | Domain entities, enums, contracts                          |
| AryamanBMS.Utilities    | Pure helpers, constants, extensions                        |

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

The Web project currently contains most application Models, Controllers,
Repositories, Services, ApplicationDbContext, and EF mappings.

The refactor will move responsibility—not alter business behaviour.

## Migration Definition of Done

A module is complete only when:

1. Its code is in the correct target project.
2. The Web controller contains only HTTP/UI orchestration.
3. The solution builds successfully.
4. Existing user workflow checks pass.
5. The module checkpoint is committed and pushed.

## Completed Refactor Slices

- Location and Country: location entities, DbContext, and repository layering.
- Company Document Category: shared entity move.
- Financial Constants and Financial Year: constants moved to Models; service moved to Business.
- Attendance Calendar and Working Days: calendar entities, context, repository, options, and service layering.
- Password Change Logs: entity, context, repository, service, and controller consumers layered.
- Company Profile: entity, context, repository, registration, and document-service consumer layered.
- GST Configuration: entity, context, repository, registration, and invoice consumer layered.
- Financial Audit Documents: entity, context, repository, and registration layered.
- Payroll Configuration: payroll policy, payroll period lock, and professional-tax slabs moved to Models and owned by `PayrollConfigurationDbContext`.

## Deferred Aggregates

The following remain in the Web-owned EF context intentionally. They require an aggregate-level redesign to preserve EF relationships and atomic workflows:

- Identity-bound records: Login History and other entities that navigate to `ApplicationUserModel`.
- Leave: Leave Balance, Leave Type, Leave Applications, and related attendance updates.
- Payroll processing: Salary Payment Batches, Salary Records, and Financial Sequences.
- Office Assets: assets, assignment history, documents, maintenance, and verification records.
- GST LUT documents and other file-upload workflows with relationship dependencies.

## Verification

- Project-reference direction audited: no lower-layer project references the Web project.
- No migrations or database schema changes were introduced.
- Each completed slice was built with `dotnet build AryamanBMS.slnx --no-restore`.
- Affected workflows were manually verified during each slice.
