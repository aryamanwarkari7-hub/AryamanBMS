# AryamanBMS

AryamanBMS is an ASP.NET Core MVC business management system for internal operations across HR, attendance, leave, payroll, projects, accounts, statutory compliance, documents, assets, and notifications.

The application is built as a .NET 8 web application with Razor views, ASP.NET Core Identity, Entity Framework Core, MySQL, Bootstrap, jQuery, SignalR, ClosedXML/OpenXML, and QuestPDF.

## Current Implementation

The solution contains multiple projects, but the active implementation currently lives primarily inside the main web project, `AryamanBMS`.

```text
AryamanBMS.slnx
├── AryamanBMS/                 Main ASP.NET Core MVC web application
├── AryamanBMS.Business/        Supporting class library, mostly structural
├── AryamanBMS.Database/        Supporting class library, mostly structural
├── AryamanBMS.Models/          Supporting class library, mostly structural
├── AryamanBMS.Repositories/    Supporting class library, mostly structural
└── AryamanBMS.Utilities/       Supporting class library, mostly structural
```

Within the web project, the application follows a conventional MVC layout:

```text
AryamanBMS/
├── Areas/Identity/             Scaffolded Identity Razor pages
├── Controllers/                MVC controllers for each business module
├── Data/                       EF Core DbContext, Identity user seed, location seed data
├── Extensions/                 Shared query/view helpers
├── Hubs/                       SignalR hubs
├── Middleware/                 Request middleware
├── Migrations/                 EF Core migrations
├── Models/                     Domain and persistence models
├── Repositories/               Repository interfaces and implementations
├── Services/                   Domain services and background service classes
├── SQL/                        Manual and migrated SQL scripts
├── ViewComponents/             Reusable MVC view components
├── ViewModels/                 UI-specific models
├── Views/                      Razor views
└── wwwroot/                    Static assets, frontend libraries, templates, uploads
```

## Technology Stack

|         Area            |                           Implementation                       |
|         ---             |                           -------------                        |
| Runtime                 | .NET 8                                                         |
| Web framework           | ASP.NET Core MVC                                               |
| Views                   | Razor views                                                    |
| Authentication          | ASP.NET Core Identity                                          |
| Authorization           | Role-based authorization attributes                            |
| ORM                     | Entity Framework Core 8                                        |
| Database provider       | Pomelo Entity Framework Core provider for MySQL                |
| Database                | MySQL 8 compatible                                             |
| Realtime updates        | SignalR                                                        |
| Frontend                | Bootstrap, Bootstrap Icons, jQuery, jQuery Validation          |
| Excel handling          | ClosedXML, DocumentFormat.OpenXml                              |
| PDF/document generation | QuestPDF                                                       |
| File storage            | Local filesystem, primarily under `App_Data` for private files |

## Application Startup

The application starts from:

```text
AryamanBMS/Program.cs
```

Startup responsibilities include:

- Loading the `DefaultConnection` connection string.
- Registering `ApplicationDbContext` with MySQL.
- Registering ASP.NET Core Identity using `ApplicationUserModel` and `IdentityRole`.
- Configuring cookie routes:
  - Login: `/Account/Login`
  - Access denied: `/Account/AccessDenied`
- Registering repository and service dependencies.
- Registering MVC controllers with views.
- Registering SignalR.
- Enabling HTTPS redirection, static files, routing, authentication, user activity middleware, and authorization.
- Mapping the default MVC route to `Account/Login`.
- Mapping the notification hub at `/notificationHub`.
- Seeding roles and an optional admin account through `DbInitializer`.

## Core Data Flow

Typical request flow:

```text
Browser
  -> MVC Controller
  -> Repository and/or Service
  -> ApplicationDbContext
  -> MySQL
  -> Razor View / JSON / File Result
```

Controllers are the main orchestration layer. Repositories expose EF-backed query access and CRUD operations. Services are used where the system has cross-cutting or heavier domain workflows such as file handling, notifications, GST calculations, salary imports, project timelines, document generation, and login history.

## Authentication And Roles

The system uses ASP.NET Core Identity with a custom user model:

```text
AryamanBMS/Models/ApplicationUserModel.cs
```

`ApplicationUserModel` extends `IdentityUser` with:

- `FullName`
- `IsActive`
- `ProfilePhotoPath`
- activity status fields
- notification preferences
- `CreatedOn`

The startup seed process creates these roles:

- `Admin`
- `HR`
- `Finance`
- `Employee`
- `ProjectManager`
- `Master`

`Master` is a restricted management role. It is intended for access to Dashboard, Leaves, Attendance, Project Management, Proposals and Templates, PO/WO, Receipts, Billing Milestones, Expense Vouchers, Vendor Payments, Calendar, and Holiday Register. It does not expose user/role administration, employee master setup, salary, compliance, client/vendor setup, invoices, receivables, credit notes, or debit notes through the main navigation.

An admin user can be seeded only when these configuration keys are present:

```text
SeedAdmin:UserName
SeedAdmin:Email
SeedAdmin:Password
```

The main login flow is implemented in:

```text
AryamanBMS/Controllers/AccountController.cs
```

It handles:

- login
- logout
- inactive-user checks
- lockout-on-failure
- password reset
- profile display
- profile photo upload
- password change
- user activity status
- login history recording
- admin login notifications

## Major Functional Modules

### Dashboard

Entry point for authenticated users after login. Admin, HR, Finance, and Master users are routed toward the main dashboard. Employee-only users are routed through attendance/profile-oriented flows.

Key files:

```text
AryamanBMS/Controllers/DashboardController.cs
AryamanBMS/ViewModels/MainDashboardViewModel.cs
AryamanBMS/Views/Dashboard/
```

### Account, Users, Roles, And Security Logs

Supports authentication, profile management, password changes, admin user management, role management, login history, and password change audit logs.

Key files:

```text
AryamanBMS/Controllers/AccountController.cs
AryamanBMS/Controllers/UserController.cs
AryamanBMS/Controllers/RoleController.cs
AryamanBMS/Controllers/LoginHistoryController.cs
AryamanBMS/Controllers/PasswordChangeLogController.cs
AryamanBMS/Data/DbInitializer.cs
AryamanBMS/Services/LoginHistoryService.cs
```

### Employee Management

Handles employee records, departments, designations, employee profiles, academics, previous employment, documents, and Identity-user linkage.

Key files:

```text
AryamanBMS/Controllers/EmployeeController.cs
AryamanBMS/Controllers/DepartmentController.cs
AryamanBMS/Controllers/DesignationController.cs
AryamanBMS/Models/EmployeeModel.cs
AryamanBMS/Models/EmployeeAcademicModel.cs
AryamanBMS/Models/EmployeeDocumentModel.cs
AryamanBMS/Models/EmployeePreviousEmploymentModel.cs
AryamanBMS/Services/EmployeeDocumentService.cs
```

### Attendance

Tracks attendance records, employee self attendance, summaries, registers, dashboards, and admin/HR adjustments. Attendance supports full-day and half-day values so salary pay days can include decimal values such as `26.5`. Weekly offs are configuration-driven, and uploaded active holidays from the Holiday Register are treated as office holidays for attendance blocking and pay-day calculations.

Key files:

```text
AryamanBMS/Controllers/AttendanceController.cs
AryamanBMS/Models/AttendanceModel.cs
AryamanBMS/Repositories/AttendanceRepository.cs
AryamanBMS/ViewModels/AttendanceDashboardViewModel.cs
AryamanBMS/ViewModels/AttendanceSummaryViewModel.cs
```

### Holiday Register

Supports yearly office holiday setup through a register page, downloadable blank Excel template, Excel import, filtered export, and Admin/HR/Master access. Holidays are stored as master data and are used by Calendar, Attendance, and Salary Pay Days.

Key files:

```text
AryamanBMS/Controllers/HolidayController.cs
AryamanBMS/Models/HolidayModel.cs
AryamanBMS/Services/HolidayExcelImportService.cs
AryamanBMS/ViewModels/HolidayImportResult.cs
AryamanBMS/Views/Holiday/
AryamanBMS/wwwroot/templates/HolidayTemplate.xlsx
```

### Leave And Comp-Off

Supports leave type setup, leave applications, half-day leave, leave balances, approvals, cancellations, comp-off credits, and comp-off usage. Comp Off is dynamic and is based on approved Comp Off credits, not fixed yearly days in Leave Type setup.

Key files:

```text
AryamanBMS/Controllers/LeaveTypeController.cs
AryamanBMS/Controllers/LeaveApplicationController.cs
AryamanBMS/Controllers/LeaveBalanceController.cs
AryamanBMS/Controllers/CompOffCreditController.cs
AryamanBMS/Models/LeaveTypeModel.cs
AryamanBMS/Models/LeaveApplicationModel.cs
AryamanBMS/Models/LeaveBalanceModel.cs
AryamanBMS/Models/CompOffCreditModel.cs
AryamanBMS/Models/CompOffUsageModel.cs
```

### Payroll And Salary

Covers salary records, salary structure, Excel salary imports, salary dashboard, payslips, salary advances, payment batches, attendance summaries, payroll policy, payroll locks, and full-and-final settlement. Salary attendance summaries include uploaded active holidays as payable holiday days and exclude them from working-day calculations.

Key files:

```text
AryamanBMS/Controllers/SalaryController.cs
AryamanBMS/Controllers/SalaryStructureController.cs
AryamanBMS/Controllers/SalaryAdvanceController.cs
AryamanBMS/Controllers/SalaryPaymentBatchController.cs
AryamanBMS/Controllers/FullAndFinalSettlementController.cs
AryamanBMS/Services/SalaryExcelImportService.cs
AryamanBMS/Services/SalaryAttendanceSummaryService.cs
AryamanBMS/wwwroot/templates/SalaryTemplate.xlsx
```

### Projects, Tasks, Meetings, Timeline, And Risks

Supports project setup, project members, project tasks, task progress, project flows, timelines, communications, meetings/MOM, employee project tasks, and risk tracking.
Project access includes Admin, HR, Master, project managers, and active assigned project members.

Key files:

```text
AryamanBMS/Controllers/ProjectController.cs
AryamanBMS/Controllers/ProjectMemberController.cs
AryamanBMS/Controllers/ProjectTaskController.cs
AryamanBMS/Controllers/ProjectFlowController.cs
AryamanBMS/Controllers/ProjectTimelineController.cs
AryamanBMS/Controllers/ProjectCommunicationController.cs
AryamanBMS/Controllers/EmployeeProjectController.cs
AryamanBMS/Controllers/MOMController.cs
AryamanBMS/Controllers/RiskController.cs
AryamanBMS/Services/ProjectAccessService.cs
AryamanBMS/Services/ProjectTimelineService.cs
```

### Calendar

Provides a work calendar with month, week, day, and list views. Calendar events are aggregated from holidays, leave applications, attendance exceptions, project tasks, meetings, billing milestones, and manual calendar entries. Admin, HR, and Master users can add and edit manual calendar events; employees can view their own schedule and shared holiday/manual entries.

Key files:

```text
AryamanBMS/Controllers/CalendarController.cs
AryamanBMS/Models/CalendarManualEventModel.cs
AryamanBMS/Services/CalendarService.cs
AryamanBMS/ViewModels/CalendarEventViewModel.cs
AryamanBMS/ViewModels/CalendarManualEventInputViewModel.cs
AryamanBMS/Views/Calendar/
AryamanBMS/wwwroot/css/calendar.css
AryamanBMS/wwwroot/js/calendar.js
```

### Accounts, Billing, Receivables, And Documents

Supports company profile, clients, client communications, proposal templates, proposals, purchase/work orders, billing milestones, invoices, advance receipts, payment receipts, receivables, credit notes, debit notes, and document versioning.

Key files:

```text
AryamanBMS/Controllers/AccountsFinanceController.cs
AryamanBMS/Controllers/ClientController.cs
AryamanBMS/Controllers/ClientCommunicationController.cs
AryamanBMS/Controllers/ProposalTemplateController.cs
AryamanBMS/Controllers/ProposalController.cs
AryamanBMS/Controllers/PurchaseOrderController.cs
AryamanBMS/Controllers/BillingMilestoneController.cs
AryamanBMS/Controllers/InvoiceController.cs
AryamanBMS/Controllers/AdvanceReceiptController.cs
AryamanBMS/Controllers/PaymentReceiptController.cs
AryamanBMS/Controllers/ReceivablesController.cs
AryamanBMS/Controllers/CreditNoteController.cs
AryamanBMS/Controllers/DebitNoteController.cs
AryamanBMS/Services/ProposalDocumentService.cs
AryamanBMS/Services/InvoiceDocumentService.cs
AryamanBMS/Services/FinancialYearService.cs
```

### Purchases, Expenses, Vendors, And Assets

Supports vendors, expense categories, expense vouchers, expense documents, vendor payments, purchase reports, and office assets with assignment, maintenance, document, and verification tracking.

Key files:

```text
AryamanBMS/Controllers/VendorController.cs
AryamanBMS/Controllers/ExpenseCategoryController.cs
AryamanBMS/Controllers/ExpenseVoucherController.cs
AryamanBMS/Controllers/VendorPaymentController.cs
AryamanBMS/Controllers/PurchaseReportController.cs
AryamanBMS/Controllers/OfficeAssetController.cs
AryamanBMS/Repositories/OfficeAssetRepository.cs
```

### Compliance And Statutory

Supports GST, PF, ESIC, PT, company documents, document categories, financial audit documents, and notices.

Key files:

```text
AryamanBMS/Controllers/GstController.cs
AryamanBMS/Controllers/PfController.cs
AryamanBMS/Controllers/EsicController.cs
AryamanBMS/Controllers/PtController.cs
AryamanBMS/Controllers/CompanyProfileController.cs
AryamanBMS/Controllers/CompanyDocumentController.cs
AryamanBMS/Controllers/CompanyDocumentCategoryController.cs
AryamanBMS/Controllers/FinancialAuditDocumentController.cs
AryamanBMS/Controllers/NoticeController.cs
AryamanBMS/Services/GstCalculationService.cs
AryamanBMS/Services/GstDashboardService.cs
```

### Notifications And Activity

Supports persistent notifications, unread counts, SignalR realtime delivery, login notifications, user/security notifications, leave and Comp Off notifications, attendance updates and reminders, salary notifications, project member notifications, holiday import notifications, and user activity state.

Key files:

```text
AryamanBMS/Controllers/NotificationController.cs
AryamanBMS/Services/NotificationService.cs
AryamanBMS/Services/Background/HrNotificationReminderBackgroundService.cs
AryamanBMS/Hubs/NotificationHub.cs
AryamanBMS/Middleware/UserActivityMiddleware.cs
AryamanBMS/ViewComponents/NotificationBellViewComponent.cs
AryamanBMS/wwwroot/js/notification-realtime.js
AryamanBMS/wwwroot/js/activity-heartbeat.js
```

### Location Data

Supports state, city/district, and pincode master data.

Key files:

```text
AryamanBMS/Controllers/LocationController.cs
AryamanBMS/Data/all_india_pincode_directory.csv
AryamanBMS/Data/README_ALL_INDIA_LOCATION_SEED.txt
AryamanBMS/SQL/Migrated/SEED_ALL_INDIA_LOCATION_FROM_CSV.sql
```

## Database

The EF Core context is:

```text
AryamanBMS/Data/ApplicationDbContext.cs
```

It inherits from:

```csharp
IdentityDbContext<ApplicationUserModel>
```

The context defines DbSets and mappings for Identity, HR, attendance, holidays, leave, payroll, calendar events, projects, meetings, risks, accounts, purchases, GST, statutory modules, company documents, audit documents, office assets, notices, notifications, login history, and password change logs.

The current project contains both EF migrations and manual SQL scripts. The SQL folder is important because several business modules appear to have schema/data scripts outside the small EF migration set.

## Configuration

The application requires a `DefaultConnection` connection string.

Example shape:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "server=localhost;database=aryamanbms;user=YOUR_USER;password=YOUR_PASSWORD;"
  }
}
```

Optional admin seed configuration:

```json
{
  "SeedAdmin": {
    "UserName": "admin",
    "Email": "admin@example.com",
    "Password": "ChangeThisPassword"
  }
}
```

Do not commit real credentials. Use user secrets, environment variables, or deployment secret storage for credentials.

Environment variable examples:

```text
ConnectionStrings__DefaultConnection
SeedAdmin__UserName
SeedAdmin__Email
SeedAdmin__Password
```

Attendance calendar configuration:

```json
{
  "Attendance": {
    "WeeklyOffDays": [ "Sunday" ],
    "OfficeHolidays": [
      "2026-01-26",
      "2026-08-15"
    ]
  }
}
```

If `Attendance:WeeklyOffDays` is not configured, Sunday is treated as the default weekly off. Office holidays can still be configured through `Attendance:OfficeHolidays`, but the primary holiday source is now the Holiday Register (`TableHoliday`). Manually marked `H` attendance records are also respected.

Notification reminder timing:

```json
{
  "Notifications": {
    "AttendanceMissingAfter": "10:30:00",
    "CheckOutMissingAfter": "18:30:00"
  }
}
```

## Local Development

Prerequisites:

- .NET 8 SDK
- MySQL 8 compatible server
- Visual Studio 2022, JetBrains Rider, or VS Code

Restore dependencies:

```powershell
dotnet restore AryamanBMS.slnx
```

Build:

```powershell
dotnet build AryamanBMS.slnx
```

Run the web application:

```powershell
dotnet run --project AryamanBMS/AryamanBMS.csproj
```

Development launch profiles are defined in:

```text
AryamanBMS/Properties/launchSettings.json
```

Configured development URLs include:

```text
http://localhost:5263
https://localhost:7299
```

## Entity Framework

A local tool manifest exists at:

```text
AryamanBMS/dotnet-tools.json
```

It declares `dotnet-ef`.

Restore local tools:

```powershell
dotnet tool restore --tool-manifest AryamanBMS/dotnet-tools.json
```

Common EF command shape:

```powershell
dotnet ef database update --project AryamanBMS/AryamanBMS.csproj
```

Review the `SQL` folder before assuming EF migrations alone represent the full database setup.

Latest manual schema script:

```text
AryamanBMS/SQL/Manual_Half_Day_Leave_Attendance_Changes.sql
```

## Static Assets And Frontend

Static files are located under:

```text
AryamanBMS/wwwroot
```

Important frontend areas:

- `wwwroot/css`: global, layout, module, and component styles
- `wwwroot/js`: module-specific behavior
- `wwwroot/lib`: vendored frontend libraries
- `wwwroot/templates`: import templates, including salary and holiday Excel templates
- `wwwroot/uploads`: public upload paths such as profile photos

Shared layout and navigation:

```text
AryamanBMS/Views/Shared/_Layout.cshtml
AryamanBMS/Views/Shared/_Sidebar.cshtml
AryamanBMS/Views/Shared/_Navbar.cshtml
```

## File Storage

The shared file storage service is:

```text
AryamanBMS/Services/FileStorageService.cs
```

It stores files under the application content root `App_Data`, validates folder names, restricts extensions, limits file size, and guards against path traversal when resolving physical paths.

Some features also use public static paths under `wwwroot`, such as profile photos in:

```text
AryamanBMS/wwwroot/uploads/profile-photos
```

## Background Services

Background service classes exist for reminders:

```text
AryamanBMS/Services/Background/TaskReminderBackgroundService.cs
AryamanBMS/Services/Background/InvoiceReminderBackgroundService.cs
AryamanBMS/Services/Background/HrNotificationReminderBackgroundService.cs
```

`HrNotificationReminderBackgroundService` is registered in `Program.cs` for HR reminders such as Comp Off expiry, missing check-in, and missing check-out. Task and invoice reminder services are present separately.

## Publishing To IIS

Publish from the working machine:

```powershell
dotnet publish AryamanBMS/AryamanBMS.csproj -c Release -o ./Publish
```

Before replacing IIS files, run required SQL scripts on the server database and set the production connection string outside committed configuration:

```text
ConnectionStrings__DefaultConnection
```

Current deployment-sensitive tables include `TableHoliday`, `TableCalendarManualEvent`, and the current Project Management tables. When publishing to IIS, the server database schema must match the deployed models before testing Calendar, Holiday, Salary Pay Days, or Project Management pages.

## Repository Notes

Generated build output and local IDE state are ignored, including `bin`, `obj`, `.vs`, `.tmp-build`, and `.codex-build`.

The main implementation should be understood from source files under the web project rather than from the empty supporting class-library project folders.

## Areas Requiring Extra Attention For Future Work

- Keep secrets out of committed configuration files.
- Verify role usage when adding protected controllers or views.
- Check both EF migrations and SQL scripts before changing database schema.
- Preserve object-level access checks for employee, salary, project, document, and finance records.
- Be careful around modules that combine controller logic, repository queries, and direct `ApplicationDbContext` usage.
- Revalidate file upload and download paths when adding document features.

## Project Status

This README reflects the current static project structure and implementation after the Holiday Register, Calendar, and holiday attendance/pay-day integration work. It is intended as the primary GitHub-facing project overview and onboarding reference.
