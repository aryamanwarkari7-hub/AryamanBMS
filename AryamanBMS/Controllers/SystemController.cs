using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AryamanBMS.Controllers
{
    [Authorize]
    public class SystemController : Controller
    {
        public IActionResult ComingSoon()
        {
            return View("~/Views/Shared/ComingSoon.cshtml");
        }

        [AllowAnonymous]
        public IActionResult NotFoundPage()
        {
            return View("~/Views/Shared/NotFound.cshtml");
        }
    }
}