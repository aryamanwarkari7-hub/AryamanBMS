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

        public BirthdayController(
            IEmployeeRepository employeeRepository)
        {
            _employeeRepository = employeeRepository;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int? month)
        {
            int? selectedMonth =
                month.HasValue &&
                month.Value >= 1 &&
                month.Value <= 12
                    ? month.Value
                    : null;

            var employeeQuery = _employeeRepository.Employees
                .AsNoTracking()
                .Where(x =>
                    x.IsActive &&
                    x.DateOfBirth.HasValue);

            if (selectedMonth.HasValue)
            {
                employeeQuery = employeeQuery.Where(x =>
                    x.DateOfBirth!.Value.Month == selectedMonth.Value);
            }

            var employees = await employeeQuery.ToListAsync();

            var birthdays = employees
                .Select(x => new BirthdayListItemViewModel
                {
                    EmployeeName = x.FullName ?? "Employee",
                    Day = x.DateOfBirth!.Value.Day,
                    Month = x.DateOfBirth.Value.Month
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