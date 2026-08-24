using AryamanBMS.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class PasswordChangeLogController : Controller
    {
        private readonly IPasswordChangeLogService
            _passwordChangeLogService;

        public PasswordChangeLogController(
            IPasswordChangeLogService passwordChangeLogService)
        {
            _passwordChangeLogService =
                passwordChangeLogService;
        }

        public async Task<IActionResult> Index()
        {
            var logs =
                await _passwordChangeLogService.GetAllAsync();

            return View(logs);
        }
    }
}