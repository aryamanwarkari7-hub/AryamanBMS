using AryamanBMS.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class PasswordChangeLogController : Controller
    {
        #region Actions

        private readonly ApplicationDbContext _context;

        public PasswordChangeLogController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var logs = await _context.PasswordChangeLogs
                .AsNoTracking()
                .OrderByDescending(x => x.ChangedOn)
                .ToListAsync();

            return View(logs);
        }
        #endregion
    }
}
