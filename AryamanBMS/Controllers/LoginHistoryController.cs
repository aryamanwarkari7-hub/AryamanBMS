using AryamanBMS.Data;
using AryamanBMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class LoginHistoryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LoginHistoryController(
            ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string? searchText,
            string? eventType,
            string? result,
            int? month,
            int? year,
            int page = 1)
        {
            const int pageSize = 15;

            if (page < 1)
            {
                page = 1;
            }

            var query = _context.TableLoginHistory
                .AsNoTracking()
                .Include(x => x.User)
                .AsQueryable();

            var availableYears = await _context.TableLoginHistory
                .AsNoTracking()
                .Select(x => x.OccurredOn.Year)
                .Distinct()
                .OrderByDescending(x => x)
                .ToListAsync();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                string search = searchText.Trim();

                query = query.Where(x =>
                    x.AttemptedUserName.Contains(search) ||
                    (x.IpAddress != null &&
                     x.IpAddress.Contains(search)) ||
                    (x.User != null &&
                     x.User.FullName != null &&
                     x.User.FullName.Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(eventType))
            {
                query = query.Where(x =>
                    x.EventType == eventType);
            }

            if (result == "Success")
            {
                query = query.Where(x => x.IsSuccessful);
            }
            else if (result == "Failed")
            {
                query = query.Where(x => !x.IsSuccessful);
            }

            if (year.HasValue)
            {
                query = query.Where(x =>
                    x.OccurredOn.Year == year.Value);
            }

            if (month.HasValue)
            {
                query = query.Where(x =>
                    x.OccurredOn.Month == month.Value);
            }

            int totalRecords = await query.CountAsync();

            int totalPages = (int)Math.Ceiling(
                totalRecords / (double)pageSize);

            if (totalPages > 0 && page > totalPages)
            {
                page = totalPages;
            }

            var records = await query
                .OrderByDescending(x => x.OccurredOn)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var model = new LoginHistoryListViewModel
            {
                Records = records,
                SearchText = searchText,
                EventType = eventType,
                Result = result,
                Month = month,
                Year = year,
                AvailableYears = availableYears,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalRecords = totalRecords
            };

            return View(model);
        }
    }
}