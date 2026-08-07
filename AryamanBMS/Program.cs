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

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' is not configured.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseMySql(
    connectionString,
    new MySqlServerVersion(new Version(8, 0, 43))
    );
});

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
});
builder.Services.AddAuthorization();
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();

builder.Services.AddScoped<IEmployeeAcademicRepository, EmployeeAcademicRepository>();

builder.Services.AddScoped<IEmployeeDocumentRepository, EmployeeDocumentRepository>();
builder.Services.AddScoped<IEmployeePreviousEmploymentRepository, EmployeePreviousEmploymentRepository>();

builder.Services.AddScoped<ILocationRepository, LocationRepository>();

builder.Services.AddScoped<IAttendanceRepository, AttendanceRepository>();

builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();

builder.Services.AddScoped<IDesignationRepository, DesignationRepository>();

// Leave Repo
builder.Services.AddScoped<ILeaveTypeRepository, LeaveTypeRepository>();

builder.Services.AddScoped<ILeaveApplicationRepository, LeaveApplicationRepository>();

builder.Services.AddScoped<ILeaveBalanceRepository, LeaveBalanceRepository>();

builder.Services.AddScoped<ICompOffCreditRepository, CompOffCreditRepository>();

builder.Services.AddScoped<ICompOffUsageRepository, CompOffUsageRepository>();

// Salary
builder.Services.AddScoped<ISalaryRecordRepository, SalaryRecordRepository>();

// Letter
builder.Services.AddScoped<ILetterRepository, LetterRepository>();

// Project
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<IProjectMemberRepository, ProjectMemberRepository>();
builder.Services.AddScoped<IProjectTaskRepository, ProjectTaskRepository>();
builder.Services.AddScoped<IProjectFlowRepository, ProjectFlowRepository>();
builder.Services.AddScoped<IProjectTaskProgressRepository, ProjectTaskProgressRepository>();
builder.Services.AddScoped<IProjectTimelineRepository, ProjectTimelineRepository>();
builder.Services.AddScoped<IProjectCommunicationRepository,ProjectCommunicationRepository>();

// Meetings
builder.Services.AddScoped<IProjectMeetingRepository, ProjectMeetingRepository>();

//Risk
builder.Services.AddScoped<IProjectRiskRepository, ProjectRiskRepository>();

// Accounts
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
builder.Services.AddScoped<IGstSnapshotRepository, GstSnapshotRepository>();
builder.Services.AddScoped<IGstConfigurationRepository, GstConfigurationRepository>();
builder.Services.AddScoped<IGstReturnRepository, GstReturnRepository>();
builder.Services.AddScoped<IGstChallanRepository, GstChallanRepository>();
builder.Services.AddScoped<IGstItcRepository, GstItcRepository>();
builder.Services.AddScoped<IGstDocumentRepository, GstDocumentRepository>();
builder.Services.AddScoped<IFinancialAuditDocumentRepository, FinancialAuditDocumentRepository>();
builder.Services.AddScoped<IOfficeAssetRepository, OfficeAssetRepository>();
builder.Services.AddScoped<IPfRepository, PfRepository>();
builder.Services.AddScoped<IEsicRepository, EsicRepository>();
builder.Services.AddScoped<IPtRepository, PtRepository>();
builder.Services.AddScoped<INoticeRepository, NoticeRepository>();
builder.Services.AddScoped<IProposalTemplateRepository,ProposalTemplateRepository>();

// SALARY SERVICE
builder.Services.AddScoped<ISalaryExcelImportService, SalaryExcelImportService>();
builder.Services.AddScoped<ISalaryAttendanceSummaryService, SalaryAttendanceSummaryService>();

// EMPLOYEE SERVICE
builder.Services.AddScoped<IEmployeeDocumentService, EmployeeDocumentService>();

// PROJECT SERVICE

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<IProjectTimelineService, ProjectTimelineService>();

builder.Services.AddScoped<IProjectAccessService, ProjectAccessService>();

// ACCOUNTS SERVICE
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<IFinancialYearService, FinancialYearService>();
builder.Services.AddScoped<IGstCalculationService, GstCalculationService>();
builder.Services.AddScoped<IGstDashboardService, GstDashboardService>();

// PROPOSAL SERVICE
builder.Services.AddScoped< IProposalDocumentService,ProposalDocumentService>();

// INVOICE SERVICE
builder.Services.AddScoped<IInvoiceDocumentService,InvoiceDocumentService>();

// NOTIFICATION SERVICE
builder.Services.AddScoped<INotificationService,NotificationService>();

// CALENDAR SERVICE
builder.Services.AddScoped<ICalendarService, CalendarService>();

//HOLIDAY SERVICE
builder.Services.AddScoped<IHolidayExcelImportService, HolidayExcelImportService>();

// BACKGROUND SERVICE
builder.Services.AddHostedService<TaskReminderBackgroundService>();
builder.Services.AddHostedService<InvoiceReminderBackgroundService>();
builder.Services.AddHostedService<HrNotificationReminderBackgroundService>();

// LOGIN HISTORY SERVICE
builder.Services.AddScoped<ILoginHistoryService,LoginHistoryService>();

QuestPDF.Settings.License =LicenseType.Evaluation;

builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();

}

app.UseStatusCodePagesWithReExecute(
    "/System/NotFoundPage");

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();

app.UseMiddleware<UserActivityMiddleware>();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.MapHub<NotificationHub>("/notificationHub");


using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    await DbInitializer.SeedRolesAndAdminAsync(services);
}
app.Run();

