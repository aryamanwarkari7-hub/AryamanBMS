
using AryamanBMS.Models;
using AryamanBMS.Services.Interfaces;
using AryamanBMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using AryamanBMS.Repositories.Interfaces;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Employee,Admin,HR,Master")]
    public class CalendarController : Controller
    {
        private readonly ICalendarService _calendarService;

        private readonly UserManager<ApplicationUserModel> _userManager;
        private readonly ICalendarManualEventRepository _calendarManualEventRepository;

        public CalendarController(
    ICalendarService calendarService,
    ICalendarManualEventRepository calendarManualEventRepository,
    UserManager<ApplicationUserModel> userManager)
        {
            _calendarService = calendarService;
            _calendarManualEventRepository = calendarManualEventRepository;
            _userManager = userManager;
        }

        #region Index
        [HttpGet]
        public IActionResult Index(bool mine = false)
        {
            ViewBag.PersonalOnly = mine;
            return View();
        }

        #endregion

        [HttpGet]
        public async Task<IActionResult> Events(
            DateTime start,
            DateTime end,
            bool mine = false)
        {
            var events = await _calendarService.GetEventsAsync(
               User,
               start,
               end,
               mine);

            return Json(events.Select(x => new
            {
                id = x.IsManual ? $"manual-{x.Id}" : null,
                manualId = x.Id,
                isManual = x.IsManual,
                title = x.Title,
                start = x.Start,
                end = x.End,
                allDay = x.AllDay,
                display = x.Display,
                textColor = x.TextColor,
                color = x.Color,
                url = x.Url,
                type = x.Type,
                status = x.Status,
                description = $"{x.Type} | {x.Status}",
                extendedProps = new
                {
                    id = x.Id,
                    isManual = x.IsManual,
                    type = x.Type,
                    status = x.Status
                }
            }));
        }

        #region Manual Events
        [HttpGet]
        [Authorize(Roles = "Admin,HR,Master")]
        public async Task<IActionResult> ManualEvent(int id)
        {
            var item = await _calendarManualEventRepository.GetActiveByIdAsync(id);

            if (item == null)
            {
                return NotFound();
            }

            return Json(new
            {
                id = item.Id,
                title = item.Title,
                description = item.Description,
                startDateTime = item.StartDateTime,
                endDateTime = item.EndDateTime,
                isAllDay = item.IsAllDay,
                eventType = item.EventType,
                visibilityScope = item.VisibilityScope
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,HR,Master")]
        public async Task<IActionResult> SaveManualEvent(
            [FromForm] CalendarManualEventInputViewModel model)
        {
            if (!CanManageManualCalendarEvents())
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .SelectMany(x => x.Value!.Errors.Select(error =>
                        string.IsNullOrWhiteSpace(error.ErrorMessage)
                            ? $"{x.Key} is invalid."
                            : error.ErrorMessage))
                    .ToList();

                return BadRequest(string.Join(" ", errors));
            }

            if (model.EndDateTime.HasValue &&
                model.EndDateTime.Value < model.StartDateTime)
            {
                return BadRequest("End date cannot be before start date.");
            }

            var user = await _userManager.GetUserAsync(User);

            CalendarManualEventModel? item;

            if (model.Id.HasValue)
            {
                item = await _calendarManualEventRepository.GetActiveByIdAsync(model.Id.Value);

                if (item == null)
                {
                    return NotFound();
                }

                item.UpdatedByUserId = user?.Id;
                item.UpdatedOn = DateTime.Now;
            }
            else
            {
                item = new CalendarManualEventModel
                {
                    CreatedByUserId = user?.Id,
                    CreatedOn = DateTime.Now,
                    IsActive = true
                };

                await _calendarManualEventRepository.AddAsync(item);
            }

            item.Title = model.Title.Trim();
            item.Description = model.Description?.Trim();
            item.StartDateTime = model.StartDateTime;
            item.EndDateTime = model.EndDateTime;
            item.IsAllDay = model.IsAllDay;
            item.EventType = model.EventType.Trim();
            item.VisibilityScope = model.VisibilityScope.Trim();

            await _calendarManualEventRepository.SaveAsync();

            return Json(new
            {
                success = true,
                id = item.Id
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,HR,Master")]
        public async Task<IActionResult> DeleteManualEvent(int id)
        {
            if (!CanManageManualCalendarEvents())
            {
                return Forbid();
            }

            var item = await _calendarManualEventRepository.GetActiveByIdAsync(id);

            if (item == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);

            item.IsActive = false;
            item.UpdatedByUserId = user?.Id;
            item.UpdatedOn = DateTime.Now;

            await _calendarManualEventRepository.SaveAsync();

            return Json(new
            {
                success = true
            });
        }
        #endregion

        #region 
        #endregion

        #region HELPERS
        private bool CanManageManualCalendarEvents()
        {
            return User.IsInRole("Admin") ||
                   User.IsInRole("HR") ||
                   User.IsInRole("Master");
        }
        #endregion
    }
}
