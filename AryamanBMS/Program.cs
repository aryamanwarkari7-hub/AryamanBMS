using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Repositories;
using AryamanBMS.Repositories.Implementations;
using AryamanBMS.Repositories.Interfaces;
using AryamanBMS.Services;
using AryamanBMS.Services.Interface;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(
            builder.Configuration.GetConnectionString("DefaultConnection")
        )
    ));

builder.Services
    .AddIdentity<ApplicationUserModel, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

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

// Meetings
builder.Services.AddScoped<IProjectMeetingRepository, ProjectMeetingRepository>();

//Risk
builder.Services.AddScoped<IProjectRiskRepository, ProjectRiskRepository>();

// SALARY SERVICE
builder.Services.AddScoped<ISalaryExcelImportService, SalaryExcelImportService>();
builder.Services.AddScoped<ISalaryAttendanceSummaryService, SalaryAttendanceSummaryService>();

// EMPLOYEE SERVICE
builder.Services.AddScoped<IEmployeeDocumentService, EmployeeDocumentService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");


using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    await DbInitializer.SeedRolesAndAdminAsync(services);
}

app.Run();
