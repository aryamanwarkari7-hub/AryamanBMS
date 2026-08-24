using AryamanBMS.Business.Interfaces;
using AryamanBMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class LoginHistoryController : Controller
    {
        private readonly ILoginHistoryService _loginHistoryService;

        public LoginHistoryController(
            ILoginHistoryService loginHistoryService)
        {
            _loginHistoryService = loginHistoryService;
        }

        #region Actions

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

            var availableYears =
                await _loginHistoryService
                    .GetAvailableYearsAsync();

            var searchResult =
                await _loginHistoryService.SearchAsync(
                    searchText,
                    eventType,
                    result,
                    month,
                    year,
                    page,
                    pageSize);

            int totalRecords =
                searchResult.TotalRecords;

            int totalPages =
                (int)Math.Ceiling(
                    totalRecords /
                    (double)pageSize);

            if (totalPages > 0 &&
                page > totalPages)
            {
                page = totalPages;

                searchResult =
                    await _loginHistoryService.SearchAsync(
                        searchText,
                        eventType,
                        result,
                        month,
                        year,
                        page,
                        pageSize);
            }

            var model = new LoginHistoryListViewModel
            {
                Records = searchResult.Records,

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

        #endregion Actions
    }
}