# AryamanBMS

AryamanBMS is an internal business management system for HR, attendance, leave, payroll, projects, finance operations, documents, assets, and notifications.

## Project Overview

AryamanBMS centralizes everyday business operations in one role-based workspace. It replaces disconnected spreadsheets and manual follow-ups with controlled workflows for employee administration, attendance, leave approval, payroll preparation, project execution, finance operations, document management, and realtime notifications.

The system is designed for organizations that need:

- A single source of truth for employee and operational data
- Controlled access based on responsibility
- Traceable approvals and audit history
- Excel-based imports and exports for practical office workflows
- Attendance and payroll calculations that respect holidays, weekly offs, half-days, and leave balances
- A browser-based application that can be hosted on Windows IIS with MySQL

## How The Application Works

```text
User signs in
    -> Role and employee mapping are checked
    -> Sidebar and workspace show permitted features
    -> User performs an operation
    -> Controller validates authorization and input
    -> Repository/service processes the workflow
    -> MySQL stores the result
    -> Razor view, Excel file, PDF, or notification is returned
```

The application uses MVC controllers for request handling, repositories for database access, services for reusable business workflows, Razor views for the interface, and SignalR for realtime notifications.

## Features

- Role-based access for `Admin`, `HR`, `Master`, and `Employee`
- Employee records, profiles, documents, departments, and designations
- Attendance tracking with half-day support, weekly offs, holidays, and Saturday Switcher
- Holiday Register with Excel template, import, export, and calendar integration
- Leave applications, paid/unpaid leave split, financial-year leave balances, Comp Off, and Birthday Leave
- Salary registers, attendance summaries, Excel imports, payslips, advances, and payment batches
- Project management, members, tasks, timelines, meetings, communications, and risks
- Calendar with holidays, weekly offs, birthdays, leave, attendance, tasks, meetings, billing, and manual events
- Proposals, templates, purchase/work orders, billing milestones, invoices, receipts, and receivables
- Expense Vouchers for registered vendors, unregistered vendors, reimbursements, and petty-cash expenses
- Vendor payments, GST, PF, ESIC, professional tax, company documents, notices, and office assets
- Persistent and realtime notifications through SignalR
- Login history, password-change logs, account lockout, and protected document storage

## Technology

- .NET 8 and ASP.NET Core MVC
- Razor Views, Bootstrap, Bootstrap Icons, jQuery, and FullCalendar
- ASP.NET Core Identity and role-based authorization
- Entity Framework Core with MySQL 8
- SignalR for realtime notifications
- ClosedXML and OpenXML for Excel workflows
- QuestPDF for document generation

## Roles

### Admin

Full system administration and access to operational modules.

### HR

Employee administration, attendance, leave, payroll, documents, and assigned operational modules.

### Master

Restricted management access to Dashboard, Leaves, Attendance, Projects, Proposals and Templates, PO/WO, Receipts, Billing Milestones, Expense Vouchers, Vendor Payments, Calendar, Holidays, and assigned employee workspace features.

### Employee

Personal attendance, leave, paid leave balance, Comp Off, expenses, calendar, projects, tasks, notifications, and profile features.

Admin, HR, and Master users with a mapped employee record can use My Workspace for their own employee-level activities. There is currently no active `Finance` or `ProjectManager` application role.

## Typical Usage

### Initial Setup

1. Configure MySQL and the production or development connection string.
2. Start the application so Identity roles and the configured administrator can be seeded.
3. Create departments, designations, employees, and user accounts.
4. Map users to employee records when they need personal attendance, leave, calendar, or workspace access.
5. Configure weekly offs and alternate working Saturdays.
6. Import the company holiday list using `HolidayTemplate.xlsx`.
7. Configure leave types and confirm the Birthday Leave type uses code `BDL`.
8. Configure salary structures and import salary data where required.

### Daily HR And Administration Flow

1. Review the dashboard and notifications.
2. Maintain employee profiles and required documents.
3. Review attendance, missing check-ins, missing check-outs, holidays, and weekly offs.
4. Review leave applications and the employee’s paid leave balance.
5. Approve or reject leave applications; the system records paid and unpaid days.
6. Manage Comp Off credits and usage.
7. Review salary attendance summaries and payroll registers.
8. Use the Calendar to review birthdays, leave, deadlines, meetings, attendance exceptions, holidays, and manual events.

### Employee Flow

1. Sign in and open My Workspace.
2. Maintain the personal profile and view permitted documents.
3. Mark or review personal attendance.
4. Apply for full-day, half-day, regular paid, unpaid, Birthday Leave, or Comp Off leave as applicable.
5. Review the paid leave balance and the paid/unpaid split of requests.
6. View assigned projects, tasks, deadlines, calendar events, notifications, and personal expenses.

### Project And Finance Flow

1. Create or maintain projects and assign members.
2. Add tasks, deadlines, project communication, meetings, risks, and progress updates.
3. Create proposals, purchase/work orders, billing milestones, invoices, receipts, and payment records.
4. Record expenses using a registered vendor or a one-time/unregistered expense party.
5. Attach supporting documents and use authorized downloads for confidential files.
6. Review notifications and audit-related records for important changes.

## Leave And Payroll Rules

- The financial year runs from 1 April to 31 March.
- Regular paid leave entitlement is 18 days per financial year, accrued at 1.5 days per month.
- Joining after 1 April produces a prorated entitlement.
- Leave types classify the request; they do not create separate regular paid-leave pools.
- Approved applications store paid and unpaid days, including half-day values such as `0.5`.
- Birthday Leave is one additional paid day per financial year and does not reduce the regular 18-day pool.
- Comp Off is maintained separately from regular paid leave.
- Salary pay-day calculations respect approved leave, half-days, holidays, weekly offs, and working-Saturday switches.

## Audience And Use Case

AryamanBMS is intended for internal operations teams, HR departments, business administrators, project teams, and organizations that need an extensible business management platform rather than separate tools for every workflow. It demonstrates role-based authorization, MVC application design, relational data modeling, Excel interoperability, document security, background processing, realtime notifications, and IIS deployment practices.

## Requirements

- .NET 8 SDK
- MySQL 8 compatible server
- Visual Studio 2022, Rider, or VS Code

## Configuration

Keep credentials outside committed files. Configure the following environment variables or deployment secrets:

```text
ConnectionStrings__DefaultConnection
SeedAdmin__UserName
SeedAdmin__Email
SeedAdmin__Password
```

Optional attendance configuration:

```json
{
  "Attendance": {
    "WeeklyOffDays": [ "Sunday" ],
    "WorkingSaturdayNumbers": [ 1, 3, 5 ]
  }
}
```

Holiday Register data is the primary holiday source. Weekly offs and exceptional Saturdays are managed through the application configuration and Saturday Switcher.

## Run Locally

```powershell
dotnet restore AryamanBMS.slnx
dotnet build AryamanBMS.slnx
dotnet run --project AryamanBMS/AryamanBMS.csproj
```

The development application uses the launch profiles in `AryamanBMS/Properties/launchSettings.json`.

## Excel Templates

Templates are available under `AryamanBMS/wwwroot/templates`:

- `HolidayTemplate.xlsx`
- `SalaryTemplate.xlsx`

Download the relevant blank template, fill it using the existing column structure, and import it through the corresponding register page.

## File Security

Private employee and business documents are stored under `App_Data` and downloaded through authorized controller actions. Public static files are limited to intended assets such as profile photos and templates.

## IIS Deployment

Publish a Release build:

```powershell
dotnet publish AryamanBMS/AryamanBMS.csproj -c Release -o .\Publish
```

Before replacing the IIS application files:

1. Back up the production database and current application folder.
2. Apply required database scripts and verify the schema.
3. Configure the production connection string as an environment variable or IIS deployment secret.
4. Replace the published files.
5. Restart the IIS application pool.
6. Test login, roles, attendance, leave, calendar, salary, projects, documents, and notifications.

## Security Notes

- Never commit database passwords or seed-admin passwords.
- Keep production secrets outside `appsettings.json`.
- Preserve role and object-level authorization when adding modules.
- Keep confidential files outside `wwwroot`.
- Validate uploads and use authorized download actions.
