# AryamanBMS Project Analysis

Date: 2026-06-27

## Purpose Of This Document

This document is a first-pass project understanding report for the AryamanBMS prototype. It captures the current structure, technology stack, functional modules, data model, build status, and early risk/security signals. It is not yet a full vulnerability audit; it is intended to establish a reliable baseline before the deeper risk, security, and working analysis.

## Executive Summary

AryamanBMS is an ASP.NET Core MVC business management system focused on internal HR, attendance, leave, salary, project, meeting, and risk workflows. The main runnable application is the `AryamanBMS` web project, supported by several class-library projects that currently appear lightly used or mostly structural.

The application uses ASP.NET Core Identity for authentication and role-based authorization, EF Core with Pomelo MySQL as the primary database provider, Razor views for server-rendered UI, and ClosedXML for Excel salary processing. The project is in a prototype stage with several expected traits: large controllers, direct repository/query usage from controllers, hardcoded local credentials, seeded default admin credentials, committed uploaded documents, and compiler warnings that should be handled before production use.

## Solution Structure

```text
AryamanBMS.slnx
├── AryamanBMS/                 Main ASP.NET Core MVC web application
├── AryamanBMS.Business/        Class library, currently mostly scaffold/structural
├── AryamanBMS.Database/        Class library, currently mostly scaffold/structural
├── AryamanBMS.Models/          Class library, currently mostly scaffold/structural
├── AryamanBMS.Repositories/    Class library, currently mostly scaffold/structural
└── AryamanBMS.Utilities/       Class library, currently mostly scaffold/structural
```

Important note: although the solution has separate projects for business/database/models/repositories/utilities, most active code currently lives inside the web project under `AryamanBMS/Models`, `AryamanBMS/Repositories`, `AryamanBMS/Services`, `AryamanBMS/Controllers`, `AryamanBMS/ViewModels`, and `AryamanBMS/Data`.

## Technology Stack

| Area | Current Implementation |
| --- | --- |
| Runtime | .NET 8 |
| Web framework | ASP.NET Core MVC with Razor Views |
| Authentication | ASP.NET Core Identity |
| Authorization | Role-based `[Authorize]` attributes |
| ORM | Entity Framework Core 8 |
| Primary DB provider | Pomelo.EntityFrameworkCore.MySql |
| Secondary/leftover DB provider | Microsoft.EntityFrameworkCore.SqlServer package and localdb connection string |
| Excel processing | ClosedXML |
| Frontend libraries | Bootstrap, jQuery, jQuery Validation |
| Static assets | CSS, images, templates, documents under `wwwroot` |

## Runtime Configuration

The application startup is in `AryamanBMS/Program.cs`.

Current behavior:

- Registers MVC controllers with views.
- Configures `ApplicationDbContext` using MySQL and `DefaultConnection`.
- Registers ASP.NET Core Identity with `ApplicationUserModel` and `IdentityRole`.
- Configures cookie paths:
  - Login: `/Account/Login`
  - Access denied: `/Account/AccessDenied`
- Registers repositories and services in DI.
- Uses HTTPS redirection, static files, routing, authentication, and authorization.
- Maps the default route to `Dashboard/Index`.
- Seeds roles and a default admin user at startup.

## Authentication And Roles

Identity roles seeded by `DbInitializer`:

- `Admin`
- `HR`
- `Employee`

Several controllers also authorize `ProjectManager`, but this role is not part of the default seed list. That mismatch should be resolved before relying on project-management authorization.

Current default seeded admin:

- Username: configured in code
- Email: configured in code
- Password: configured in code

This is acceptable only for local/prototype use. It is a production-blocking security issue if retained in source.

## Functional Modules

### Account And Identity

Files:

- `Controllers/AccountController.cs`
- `Controllers/UserController.cs`
- `Controllers/RoleController.cs`
- `Areas/Identity/Pages/...`

Capabilities:

- Login/logout.
- Profile and password change.
- Admin user creation/editing/password reset.
- Admin role creation/deletion.
- Identity UI pages exist under `Areas/Identity`.

Notes:

- `UserController` is restricted to `Admin`.
- `RoleController` protects built-in roles from deletion.
- Login does not currently use lockout on failure.
- `ApplicationUserModel.IsActive` exists, but login flow does not appear to block inactive users.

### Employee Management

Files:

- `Controllers/EmployeeController.cs`
- `Models/EmployeeModel.cs`
- `Models/EmployeeAcademicModel.cs`
- `Models/EmployeeDocumentModel.cs`
- `Models/EmployeePreviousEmploymentModel.cs`
- `Services/Interface/EmployeeDocumentService.cs`

Capabilities:

- Employee CRUD and profile/details workflows.
- Department/designation mapping.
- Academic details.
- Statutory, joining, academic, and previous employment document handling.
- Employee-to-Identity user mapping.

Notes:

- `EmployeeController` is the largest controller at about 2,019 lines.
- The module handles sensitive personal data including Aadhaar, PAN, UAN, ESIC, emails, phone numbers, addresses, employment history, and uploaded documents.
- Employee documents are stored under `App_Data/EmployeeDocuments`.
- File extensions are restricted to PDF/JPG/JPEG/PNG and size is capped at 5 MB.

### Department And Designation

Files:

- `Controllers/DepartmentController.cs`
- `Controllers/DesignationController.cs`
- `Models/DepartmentModel.cs`
- `Models/DesignationModel.cs`

Capabilities:

- Admin/HR-managed master data for departments and designations.
- Employee records reference both.

### Attendance

Files:

- `Controllers/AttendanceController.cs`
- `Models/AttendanceModel.cs`
- `Repositories/AttendanceRepository.cs`

Capabilities:

- Attendance registration, listing, details, dashboard, summary, edit/delete workflows.
- Employee access plus Admin/HR control actions.

Notes:

- `AttendanceController` is about 875 lines.
- Build warnings point to several possible null dereferences in this controller.

### Leave And Comp-Off

Files:

- `Controllers/LeaveApplicationController.cs`
- `Controllers/LeaveBalanceController.cs`
- `Controllers/LeaveTypeController.cs`
- `Controllers/CompOffCreditController.cs`
- `Models/LeaveApplicationModel.cs`
- `Models/LeaveBalanceModel.cs`
- `Models/LeaveTypeModel.cs`
- `Models/CompOffCreditModel.cs`
- `Models/CompOffUsageModel.cs`

Capabilities:

- Leave type setup.
- Leave applications.
- Leave balances.
- Approval/cancellation workflows.
- Comp-off credit/request/usage tracking.

Notes:

- `LeaveApplicationController` is about 1,179 lines.
- Employee-only users are filtered to their own leave applications in the index path.
- Leave and comp-off state transitions should receive special attention in the next working analysis.

### Salary

Files:

- `Controllers/SalaryController.cs`
- `Models/SalaryRecordModel.cs`
- `Services/SalaryExcelImportService.cs`
- `Services/SalaryAttendanceSummaryService.cs`
- `wwwroot/templates/SalaryTemplate.xlsx`

Capabilities:

- Salary dashboard and list.
- Excel-based salary import.
- Salary edit and mark-paid flow.
- Employee salary views/payslips.
- Attendance summary support.

Notes:

- Salary generation is currently disabled in favor of uploading final salary Excel.
- Excel imports expect a sheet named `Salary` and specific columns/formulas.
- Salary data is highly sensitive and requires strict role and object-level access checks.

### Letters

Files:

- `Controllers/LetterController.cs`
- `Models/LetterModel.cs`
- `Repositories/LetterRepository.cs`
- `wwwroot/documents/letters/...`

Capabilities:

- HR/admin letter creation, listing, details, deletion.
- Generated/uploaded letter documents appear under web-accessible `wwwroot/documents/letters`.

### Projects, Meetings, Timeline, Tasks, And Risks

Files:

- `Controllers/ProjectController.cs`
- `Controllers/ProjectMemberController.cs`
- `Controllers/ProjectTaskController.cs`
- `Controllers/ProjectFlowController.cs`
- `Controllers/ProjectTimelineController.cs`
- `Controllers/MOMController.cs`
- `Controllers/RiskController.cs`
- `Models/Project*.cs`

Capabilities:

- Project CRUD/dashboard.
- Project members.
- Project tasks and progress.
- Project flow/stages.
- Project timeline events.
- Meetings, attendees, and action items.
- Risk register with probability, impact, score, severity, mitigation, contingency, and resolution tracking.

Notes:

- These controllers authorize `Admin,HR,ProjectManager`.
- `ProjectManager` is not seeded by default.
- Project lifecycle modules likely need object-level authorization beyond role membership.

### Location Data

Files:

- `Controllers/LocationController.cs`
- `Models/StateModel.cs`
- `Models/CityModel.cs`
- `Models/PincodeModel.cs`
- `Data/all_india_pincode_directory.csv`
- `SQL/Migrated/SEED_ALL_INDIA_LOCATION_FROM_CSV.sql`

Capabilities:

- State/city/pincode master data.
- All-India pincode seed data.

## Database Design Summary

Primary context:

- `Data/ApplicationDbContext.cs`

The context inherits from:

- `IdentityDbContext<ApplicationUserModel>`

Major DbSets:

- Identity user model: `ApplicationUserModel`
- HR: departments, designations, employees, academic records, employee documents, previous employment
- Location: states, cities, pincodes
- Attendance: attendances
- Leave: leave types, leave applications, leave balances, comp-off credits/usages
- Salary: salary records
- Letters: letters
- Projects: projects, members, tasks, flows, task progress, timelines
- Meetings: meetings, attendees, action items
- Risks: project risks

Important constraints configured:

- Unique state name.
- Unique city name within state.
- Unique project code.
- Unique project member by project and employee.
- Unique project task by project and task code.
- Unique project flow by project and stage order.
- Unique meeting attendee by meeting and employee.
- Decimal precision configured for budget, task hours, project progress hours, and comp-off days.

## Repository And Service Pattern

The project uses repository interfaces and implementations under:

- `AryamanBMS/Repositories`
- `AryamanBMS/Repositories/Interfaces`

Services are under:

- `AryamanBMS/Services`
- `AryamanBMS/Services/Interface`

Current pattern:

- Controllers usually inject repositories directly.
- Some controllers also inject `ApplicationDbContext` directly.
- Queryable repository properties are exposed and then further composed by controllers.
- Business logic is split between controllers and services, with a lot still in controllers.

This is workable for a prototype, but it increases the risk of duplicated business rules, inconsistent authorization, and hard-to-test workflows.

## Build Status

Command run:

```powershell
dotnet build AryamanBMS.slnx --no-restore
```

Result:

- Build succeeded.
- 45 warnings.
- 0 errors.

Important warning themes:

- Nullable reference risks in controllers, repositories, models, and views.
- `CompOffCreditController.Request()` hides `ControllerBase.Request`.
- Non-nullable view model/model properties are not initialized.
- Potential null dereferences in `AttendanceController`, `LeaveApplicationController`, `SalaryController`, project repositories/controllers, and `EmployeeController`.

## Early Risk And Security Signals

These are initial findings to guide the next audit. They should be verified in detail before remediation planning.

### High Priority

- Hardcoded MySQL connection string and password are present in `appsettings.json`.
- Default admin account and password are seeded in source code.
- Uploaded employee documents are committed under `App_Data/EmployeeDocuments`.
- Letter PDFs are stored under `wwwroot/documents/letters`, making them potentially web-accessible static files.
- Sensitive employee and salary data exists without an obvious centralized object-level authorization policy.
- `ApplicationUserModel.IsActive` is not clearly enforced during login.
- `ProjectManager` authorization is used but not seeded by default.

### Medium Priority

- Large controllers concentrate business logic and authorization-sensitive operations.
- Some controllers inject both repositories and raw `ApplicationDbContext`, weakening layering.
- Build has many nullable warnings in important workflow areas.
- Login uses `lockoutOnFailure: false`, reducing brute-force protection.
- File upload validation checks extension and size, but deeper content validation/malware scanning is not evident.
- `Html.Raw(TempData["Error"])` appears in a salary view and should be checked for XSS risk.
- There are duplicate attributes in `SalaryController`, likely from prototype edits.
- SQL Server package/connection string remain even though MySQL is the active provider.

### Operational/Repository Hygiene

- `AryamanBMS.zip` is present in the repository root.
- Generated build output under `bin/` and `obj/` exists after local build.
- Uploaded/private documents appear in project folders.
- The class-library split exists, but active implementation mostly remains in the web project.

## Working Analysis Candidates

The next phase should focus on behavior and data-flow correctness, not only code style.

Recommended order:

1. Authentication and account lifecycle
   - Login behavior.
   - Inactive user enforcement.
   - Password policy, lockout, reset flows.
   - Default admin removal strategy.

2. Authorization and object-level access
   - Employee self-access boundaries.
   - Salary self-access boundaries.
   - Leave application ownership.
   - Project manager access by assigned project, not only role.
   - Document download/delete permissions.

3. Sensitive data and file handling
   - Employee documents.
   - Letter documents.
   - Salary imports and payslips.
   - Static file exposure.
   - File type/content validation.

4. Workflow integrity
   - Leave approval/cancellation and balance updates.
   - Comp-off credit usage and reversal.
   - Attendance edit/register rules.
   - Salary import overwrite/paid-record behavior.
   - Project task/progress/timeline consistency.

5. Data model and database migration health
   - Required fields and nullable correctness.
   - Unique constraints.
   - Cascade delete risks.
   - Migration alignment with current models.
   - Manual SQL scripts versus EF migrations.

6. Build quality and maintainability
   - Resolve compiler warnings.
   - Split large controllers into services.
   - Standardize repository/service layers.
   - Add focused tests around critical business rules.

## Immediate Production Blockers

Before any production-like deployment, at minimum:

- Move all secrets to user secrets/environment variables/secure secret storage.
- Remove or rotate committed database and seeded admin credentials.
- Stop committing uploaded employee/letter documents.
- Ensure private documents are not served from `wwwroot`.
- Enforce inactive-user login blocking.
- Add lockout/rate limiting protection to login.
- Confirm role seeding includes all roles used by authorization attributes.
- Review object-level access for employee, salary, document, leave, and project records.

## Suggested Next Deliverable

The next document should be a formal risk/security audit with findings ranked by severity and mapped to:

- affected file and line,
- attack or failure scenario,
- business impact,
- recommended fix,
- test/verification approach.

