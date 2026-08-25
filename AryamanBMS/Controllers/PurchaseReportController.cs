using AryamanBMS.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin,Master")]
    public class PurchaseReportController : Controller
    {
        #region Actions

        private readonly IPurchaseReportService _purchaseReportService;

        public PurchaseReportController(
            IPurchaseReportService purchaseReportService)
        {
            _purchaseReportService = purchaseReportService;
        }

        public async Task<IActionResult> Index(
             string report = "VendorPayable",
             string? search = null,
             string sortBy = "Total",
             string sortOrder = "desc")
        {
            var reports = await _purchaseReportService.GetAsync(
                search,
                sortBy,
                sortOrder);

            ViewBag.Report = report;
            ViewBag.Search = search;
            ViewBag.SortBy = sortBy;
            ViewBag.SortOrder = sortOrder;

            ViewBag.VendorPayable = reports.VendorPayable;
            ViewBag.CategoryWise = reports.CategoryWise;
            ViewBag.VendorWise = reports.VendorWise;
            ViewBag.ProjectWise = reports.ProjectWise;
            ViewBag.DepartmentWise = reports.DepartmentWise;
            ViewBag.Reimbursements = reports.Reimbursements;
            ViewBag.Itc = reports.Itc;
            ViewBag.PaidUnpaid = reports.PaidUnpaid;
            ViewBag.Monthly = reports.Monthly;
            ViewBag.Capital = reports.Capital;

            return View();
        }

        #endregion
    }
}
