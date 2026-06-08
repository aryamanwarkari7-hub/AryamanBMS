using AryamanBMS.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Controllers
{
    
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            ViewBag.DepartmentCount =
                _context.Departments.Count();

            ViewBag.DesignationCount =
                _context.Designations.Count();

            ViewBag.EmployeeCount =
                _context.Employees.Count();

            ViewBag.ActiveEmployeeCount =
                _context.Employees.Count(e => e.IsActive);

            ViewBag.RecentEmployees =
                _context.Employees
                    .Include(e => e.Department)
                    .Include(e => e.Designation)
                    .OrderByDescending(e => e.Id)
                    .Take(5)
                    .ToList();

            return View();
        }
    }
}