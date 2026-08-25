using AryamanBMS.Data;
using AryamanBMS.Hubs;
using AryamanBMS.Middleware;
using AryamanBMS.Models;
using AryamanBMS.Repositories;
using AryamanBMS.Repositories.Implementations;
using AryamanBMS.Repositories.Interfaces;
using AryamanBMS.Services;
using AryamanBMS.Services.Background;
using AryamanBMS.Services.Interface;
using AryamanBMS.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;

// Database context for the application
using AryamanBMS.Database.Context;
using AryamanBMS.Business.Interfaces;
using AryamanBMS.Business.Services;

// =============================
// APPLICATION HOST AND CONFIGURATION
// =============================
var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' is not configured.");

// =============================
// DATABASE REGISTRATION
// =============================
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseMySql(
    connectionString,
    new MySqlServerVersion(new Version(8, 0, 43))
    );
});

builder.Services.AddDbContext<LocationDbContext>(options =>
{
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString));
});

builder.Services.AddDbContext<AttendanceCalendarDbContext>(options =>
{
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString));
});

builder.Services.AddDbContext<PasswordChangeLogDbContext>(options =>
{
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString));
});

builder.Services.AddDbContext<CompanyProfileDbContext>(options =>
{
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString));
});

builder.Services.AddDbContext<GstConfigurationDbContext>(options =>
{
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString));
});

builder.Services.AddDbContext<FinancialAuditDocumentDbContext>(options =>
{
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString));
});

builder.Services.AddDbContext<PayrollConfigurationDbContext>(options =>
{
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString));
});

builder.Services.AddDbContext<LoginHistoryDbContext>(options =>
{
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString));
});

builder.Services.AddDbContext<NotificationDbContext>(options =>
{
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString));
});

builder.Services.AddDbContext<CalendarManualEventDbContext>(options =>
{
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString));
});

builder.Services.AddDbContext<NoticeDbContext>(options =>
{
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString));
});

builder.Services.AddDbContext<CompanyDocumentDbContext>(options =>
{
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString));
});

// Attendance working-day configuration
builder.Services.Configure<WorkingDayOptions>(
    builder.Configuration.GetSection("Attendance"));

// IDENTITY, PASSWORD SECURITY, AND ACCOUNT LOCKOUT
builder.Services
    .AddIdentity<ApplicationUserModel, IdentityRole>(options =>
    {
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.MaxFailedAccessAttempts = 5;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";

    // Configure the authentication cookie lifetime and redirect paths.
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
});

builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    // A new login changes the security stamp and invalidates older sessions.
    options.ValidationInterval = TimeSpan.Zero;
});

// AUTHORIZATION POLICIES
builder.Services.AddAuthorization();


// =============================
// DATA ACCESS REPOSITORIES
// =============================
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();

builder.Services.AddScoped<IEmployeeAcademicRepository, EmployeeAcademicRepository>();

builder.Services.AddScoped<IEmployeeDocumentRepository, EmployeeDocumentRepository>();
builder.Services.AddScoped<IEmployeePreviousEmploymentRepository, EmployeePreviousEmploymentRepository>();

builder.Services.AddScoped<ILocationRepository, LocationRepository>();

builder.Services.AddScoped<IAttendanceRepository, AttendanceRepository>();

builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();

builder.Services.AddScoped<IDesignationRepository, DesignationRepository>();

// Leave repositories
builder.Services.AddScoped<ILeaveTypeRepository, LeaveTypeRepository>();

builder.Services.AddScoped<ILeaveApplicationRepository, LeaveApplicationRepository>();

builder.Services.AddScoped<ILeaveApplicationDayRepository, LeaveApplicationDayRepository>();

builder.Services.AddScoped<ILeaveBalanceRepository, LeaveBalanceRepository>();

builder.Services.AddScoped<ICompOffCreditRepository, CompOffCreditRepository>();

builder.Services.AddScoped<ICompOffUsageRepository, CompOffUsageRepository>();

builder.Services.AddScoped<IOffDayWorkRequestRepository,OffDayWorkRequestRepository>();

builder.Services.AddScoped<IWorkingDayRepository, WorkingDayRepository>();

// Salary repositories
builder.Services.AddScoped<ISalaryRecordRepository, SalaryRecordRepository>();

// Letter repositories
builder.Services.AddScoped<ILetterRepository, LetterRepository>();

// Project repositories
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<IProjectMemberRepository, ProjectMemberRepository>();
builder.Services.AddScoped<IProjectTaskRepository, ProjectTaskRepository>();
builder.Services.AddScoped<IProjectFlowRepository, ProjectFlowRepository>();
builder.Services.AddScoped<IProjectTaskProgressRepository, ProjectTaskProgressRepository>();
builder.Services.AddScoped<IProjectTimelineRepository, ProjectTimelineRepository>();
builder.Services.AddScoped<IProjectCommunicationRepository,ProjectCommunicationRepository>();

// Meeting repositories
builder.Services.AddScoped<IProjectMeetingRepository, ProjectMeetingRepository>();

// Risk repositories
builder.Services.AddScoped<IProjectRiskRepository, ProjectRiskRepository>();

// Login History
builder.Services.AddScoped<ILoginHistoryRepository, LoginHistoryRepository>();

// Password Change History
builder.Services.AddScoped<IPasswordChangeLogRepository,PasswordChangeLogRepository>();

// Notification repositories
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();

//Calendar repositories
builder.Services.AddScoped<ICalendarManualEventRepository,CalendarManualEventRepository>();

// Holiday repositories
builder.Services.AddScoped<IHolidayRepository, HolidayRepository>();

//Working-day and Saturday Switcher repositories
builder.Services.AddScoped<IWorkingDayOverrideRepository, WorkingDayOverrideRepository>();

// Accounts, documents, billing, compliance, and asset repositories
builder.Services.AddScoped<IClientRepository, ClientRepository>();
builder.Services.AddScoped<ICompanyProfileRepository, CompanyProfileRepository>();
builder.Services.AddScoped<ICompanyDocumentCategoryRepository, CompanyDocumentCategoryRepository>();
builder.Services.AddScoped<ICompanyDocumentRepository, CompanyDocumentRepository>();
builder.Services.AddScoped<IProposalRepository, ProposalRepository>();
builder.Services.AddScoped<IPurchaseOrderRepository, PurchaseOrderRepository>();
builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
builder.Services.AddScoped<IPaymentReceiptRepository, PaymentRepository>();
builder.Services.AddScoped<IExpenseCategoryRepository, ExpenseCategoryRepository>();
builder.Services.AddScoped<IExpenseVoucherRepository, ExpenseVoucherRepository>();
builder.Services.AddScoped<IVendorRepository, VendorRepository>();
builder.Services.AddScoped<IGstSnapshotRepository, GstSnapshotRepository>();
builder.Services.AddScoped<IGstConfigurationRepository, GstConfigurationRepository>();
builder.Services.AddScoped<IGstReturnRepository, GstReturnRepository>();
builder.Services.AddScoped<IGstChallanRepository, GstChallanRepository>();
builder.Services.AddScoped<IGstItcRepository, GstItcRepository>();
builder.Services.AddScoped<IGstDocumentRepository, GstDocumentRepository>();
builder.Services.AddScoped<IGstLutDocumentRepository,GstLutDocumentRepository>();
builder.Services.AddScoped<IFinancialAuditDocumentRepository, FinancialAuditDocumentRepository>();
builder.Services.AddScoped<IOfficeAssetRepository, OfficeAssetRepository>();
builder.Services.AddScoped<IPfRepository, PfRepository>();
builder.Services.AddScoped<IEsicRepository, EsicRepository>();
builder.Services.AddScoped<IPtRepository, PtRepository>();
builder.Services.AddScoped<INoticeRepository, NoticeRepository>();
builder.Services.AddScoped<IProposalTemplateRepository,ProposalTemplateRepository>();

// =============================
// APPLICATION SERVICES
// =============================
// Salary services
builder.Services.AddScoped<ISalaryExcelImportService, SalaryExcelImportService>();
builder.Services.AddScoped<IAttendanceSummaryCalculator, AttendanceSummaryCalculator>();
builder.Services.AddScoped<ISalaryAttendanceSummaryService, SalaryAttendanceSummaryService>();

// Employee services
builder.Services.AddScoped<IEmployeeDocumentService, EmployeeDocumentService>();

// Project services
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<IProjectTimelineService, ProjectTimelineService>();

builder.Services.AddScoped<IProjectAccessService, ProjectAccessService>();

// Accounts, file storage, financial-year, and GST services
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<IFinancialYearService, FinancialYearService>();
builder.Services.AddScoped<IPurchaseReportService, PurchaseReportService>();
builder.Services.AddScoped<IReceivablesReportService, ReceivablesReportService>();
builder.Services.AddScoped<IVendorService, VendorService>();
builder.Services.AddScoped<IGstCalculationService, GstCalculationService>();
builder.Services.AddScoped<IGstDashboardService, GstDashboardService>();

// Proposal document service
builder.Services.AddScoped< IProposalDocumentService,ProposalDocumentService>();

// Invoice document service
builder.Services.AddScoped<IInvoiceDocumentService,InvoiceDocumentService>();

// Notification service
builder.Services.AddScoped<INotificationService,NotificationService>();

// Calendar service
builder.Services.AddScoped<ICalendarService, CalendarService>();

// Holiday Excel service
builder.Services.AddScoped<IHolidayExcelImportService, HolidayExcelImportService>();

// Working-day and Saturday Switcher service
builder.Services.AddScoped<IWorkingDayService,WorkingDayService>();

// Background reminder services
builder.Services.AddHostedService<TaskReminderBackgroundService>();
builder.Services.AddHostedService<InvoiceReminderBackgroundService>();
builder.Services.AddHostedService<HrNotificationReminderBackgroundService>();

// Login history service
builder.Services.AddScoped<ILoginHistoryService,LoginHistoryService>();

// Password Change servic
builder.Services.AddScoped<IPasswordChangeLogService,PasswordChangeLogService>();

// PDF DOCUMENT GENERATION
QuestPDF.Settings.License = LicenseType.Evaluation;

// MVC, RAZOR VIEWS, AND REALTIME COMMUNICATION
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

// =============================
// HTTP PIPELINE
// =============================
var app = builder.Build();

// ENVIRONMENT-SPECIFIC ERROR HANDLING AND SECURITY HEADERS
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();

}

app.UseStatusCodePagesWithReExecute(
    "/System/NotFoundPage");

// HTTPS, STATIC FILES, AND ROUTING
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// AUTHENTICATION, ACTIVITY TRACKING, AND AUTHORIZATION
app.UseAuthentication();

app.UseMiddleware<UserActivityMiddleware>();

app.UseAuthorization();

// MVC DEFAULT ROUTE AND SIGNALR NOTIFICATION HUB
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.MapHub<NotificationHub>("/notificationHub");


// =============================
// DATABASE ROLE AND ADMIN SEEDING
// =============================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    await DbInitializer.SeedRolesAndAdminAsync(services);
}
app.Run();

