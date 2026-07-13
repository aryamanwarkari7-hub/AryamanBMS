using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Employee")]
    public class EmployeeProjectController : Controller
    {
        private readonly UserManager<ApplicationUserModel> _userManager;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IProjectTaskRepository _projectTaskRepository;

        public EmployeeProjectController(
            UserManager<ApplicationUserModel> userManager,
            IEmployeeRepository employeeRepository,
            IProjectTaskRepository projectTaskRepository)
        {
            _userManager = userManager;
            _employeeRepository = employeeRepository;
            _projectTaskRepository = projectTaskRepository;
        }

        [HttpGet]
        public async Task<IActionResult> MyTasks(
            string? searchText,
            string? status,
            string? priority)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var employee = await _employeeRepository.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ApplicationUserId == user.Id);

            if (employee == null)
            {
                TempData["Error"] = "Employee profile not found.";
                return RedirectToAction("MyDashboard", "Employee");
            }

            var query = _projectTaskRepository.ProjectTasks
                .AsNoTracking()
                .Include(x => x.Project)
                .Include(x => x.AssignedEmployee)
                .Where(x =>
                    x.IsActive &&
                    x.AssignedEmployeeId == employee.Id);

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                string search = searchText.Trim();

                query = query.Where(x =>
                    x.TaskCode.Contains(search) ||
                    x.TaskTitle.Contains(search) ||
                    (x.Project != null &&
                     x.Project.ProjectName.Contains(search)) ||
                    (x.Project != null &&
                     x.Project.ProjectCode.Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(x => x.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(priority))
            {
                query = query.Where(x => x.Priority == priority);
            }

            var tasks = await query
                .OrderBy(x => x.Status == "Completed")
                .ThenBy(x => x.DueDate ?? DateTime.MaxValue)
                .ThenByDescending(x => x.Priority == "Critical")
                .ThenByDescending(x => x.Priority == "High")
                .ToListAsync();

            ViewBag.SearchText = searchText;
            ViewBag.Status = status;
            ViewBag.Priority = priority;

            return View(tasks);
        }

        [HttpGet]
        public async Task<IActionResult> TaskDetails(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var employee = await _employeeRepository.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ApplicationUserId == user.Id);

            if (employee == null)
            {
                TempData["Error"] = "Employee profile not found.";
                return RedirectToAction("MyDashboard", "Employee");
            }

            var task = await _projectTaskRepository.ProjectTasks
                .AsNoTracking()
                .Include(x => x.Project)
                .Include(x => x.AssignedEmployee)
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.IsActive &&
                    x.AssignedEmployeeId == employee.Id);

            if (task == null)
            {
                return Forbid();
            }

            return View(task);
        }
    }
}