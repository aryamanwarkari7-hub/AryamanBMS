using AryamanBMS.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AryamanBMS.ViewModels
{
    public class PurchaseOrderViewModel
    {
        public PurchaseOrderModel Order { get; set; } = new PurchaseOrderModel
        {
            OrderDate = DateTime.Today,
            OrderType = "PO",
            Currency = "INR",
            Status = "Open"
        };

        public IFormFile? UploadFile { get; set; }

        public IEnumerable<SelectListItem> Clients { get; set; } =
            Enumerable.Empty<SelectListItem>();

        public IEnumerable<SelectListItem> AcceptedProposals { get; set; } =
            Enumerable.Empty<SelectListItem>();

        public IEnumerable<SelectListItem> OrderTypes { get; set; } =
            new List<SelectListItem>
            {
                new SelectListItem("Purchase Order", "PO"),
                new SelectListItem("Work Order", "WO")
            };

        public IEnumerable<SelectListItem> Statuses { get; set; } =
            new List<SelectListItem>
            {
                new SelectListItem("Open", "Open"),
                new SelectListItem("In Progress", "InProgress"),
                new SelectListItem("Delivered", "Delivered"),
                new SelectListItem("Closed", "Closed"),
                new SelectListItem("Cancelled", "Cancelled")
            };
    }
}
