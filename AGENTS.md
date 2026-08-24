# Repository Guidelines

## Project Structure & Module Organization

`AryamanBMS.slnx` is the solution entry point. The `AryamanBMS/` project contains the ASP.NET Core MVC application: controllers, Razor views, application data setup, view models, and static assets under `wwwroot/`. Supporting projects separate responsibilities: `AryamanBMS.Business/` for workflows and services, `AryamanBMS.Repositories/` for persistence, `AryamanBMS.Database/` for database concerns, `AryamanBMS.Models/` for shared domain models, and `AryamanBMS.Utilities/` for cross-cutting helpers. Keep dependencies flowing through these layers; consult `ARCHITECTURE_REFACTOR_PLAN.md` before moving types between projects.

## Build, Test, and Development Commands

Run commands from the repository root:

```powershell
dotnet restore AryamanBMS.slnx
dotnet build AryamanBMS.slnx
dotnet run --project AryamanBMS/AryamanBMS.csproj
dotnet publish AryamanBMS/AryamanBMS.csproj -c Release -o .\Publish
```

Restore downloads dependencies, build compiles all projects, run starts the local web app using `Properties/launchSettings.json`, and publish creates an IIS-ready release. Configure MySQL and development secrets before exercising database-backed features.

## Coding Style & Naming Conventions

Use standard C# conventions: four-space indentation, file-scoped or block namespaces consistent with the surrounding file, PascalCase for types and public members, camelCase for locals and parameters, and an `Async` suffix for asynchronous methods. Nullable reference types and implicit usings are enabled. Keep controllers thin, place reusable workflows in Business, persistence logic in Repositories, and shared entities in Models. Match existing Razor, JavaScript, and CSS organization; use kebab-case for new asset filenames such as `invoice-form.js`.

## Testing Guidelines

No automated test project is currently committed. For every change, run `dotnet build AryamanBMS.slnx --no-restore` and manually verify the affected role and workflow. Pay particular attention to authorization, attendance/leave calculations, document downloads, Excel/PDF generation, and SignalR notifications. New test projects should use names such as `AryamanBMS.Business.Tests`, with test files named `<Subject>Tests.cs` and behavior-focused test methods.

## Commit & Pull Request Guidelines

Recent history uses short, imperative summaries such as `Move location models to shared models`. Keep each commit focused and describe the outcome, not the editing process. Create branches like `feature/short-change-name`; `bms-system-main` is protected. Pull requests should explain the change, affected modules and roles, configuration or schema steps, and verification performed. Link relevant issues and include screenshots for visible UI changes. Never commit credentials, production data, generated publish output, or private documents.
