using AryamanBMS.Repositories.Interfaces;
using AryamanBMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Controllers
{
    [Authorize]
    public class BirthdayController : Controller
    {
        private readonly IEmployeeRepository _employeeRepository;

        public BirthdayController(IEmployeeRepository employeeRepository)
        {
            _employeeRepository = employeeRepository;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int? month)
        {
            int? selectedMonth =
                month.HasValue && month.Value >= 1 && month.Value <= 12 ? month.Value : null;

            var today = DateTime.Today;

            var employeeQuery = _employeeRepository
                .Employees.AsNoTracking()
                .Include(x => x.Department)
                .Include(x => x.Designation)
                .Include(x => x.ApplicationUser)
                .Where(x => x.IsActive && x.DateOfBirth.HasValue);

            if (selectedMonth.HasValue)
            {
                employeeQuery = employeeQuery.Where(x =>
                    x.DateOfBirth!.Value.Month == selectedMonth.Value
                );
            }

            var employees = await employeeQuery.ToListAsync();

            var birthdays = employees
                .Select(x =>
                {
                    var dob = x.DateOfBirth!.Value;
                    var thisYearBday = new DateTime(today.Year, dob.Month, dob.Day);
                    if (thisYearBday < today)
                    {
                        thisYearBday = thisYearBday.AddYears(1);
                    }
                    int daysUntil = (thisYearBday - today).Days;
                    bool isToday = dob.Month == today.Month && dob.Day == today.Day;

                    return new BirthdayListItemViewModel
                    {
                        EmployeeId = x.Id,
                        EmployeeCode = x.EmployeeCode ?? string.Empty,
                        EmployeeName = x.FullName ?? "Employee",
                        DepartmentName = x.Department?.DepartmentName,
                        DesignationName = x.Designation?.DesignationName,
                        ProfilePhotoPath = x.ApplicationUser?.ProfilePhotoPath,
                        Day = dob.Day,
                        Month = dob.Month,
                        IsToday = isToday,
                        DaysUntil = daysUntil,
                    };
                })
                .OrderBy(x => x.Month)
                .ThenBy(x => x.Day)
                .ThenBy(x => x.EmployeeName)
                .ToList();

            ViewBag.Month = selectedMonth;
            return View(birthdays);
        }
    }
}
