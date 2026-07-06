using AryamanBMS.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AryamanBMS.ViewModels
{
    public class ProposalViewModel
    {
        public ProposalModel Proposal { get; set; } = new ProposalModel
        {
            ProposalDate = DateTime.Today,
            Currency = "INR",
            Status = "Draft"
        };

        public IFormFile? UploadFile { get; set; }

        public IEnumerable<SelectListItem> Clients { get; set; } =
            Enumerable.Empty<SelectListItem>();

        public IEnumerable<SelectListItem> Projects { get; set; } =
            Enumerable.Empty<SelectListItem>();

        public IEnumerable<SelectListItem> Statuses { get; set; } =
            new List<SelectListItem>
            {
                new SelectListItem("Draft", "Draft"),
                new SelectListItem("Sent", "Sent"),
                new SelectListItem("Under Review", "UnderReview"),
                new SelectListItem("Accepted", "Accepted"),
                new SelectListItem("Rejected", "Rejected"),
                new SelectListItem("Expired", "Expired")
            };

        public IEnumerable<SelectListItem> ProposalTemplates
        {
            get;
            set;
        } = Enumerable.Empty<SelectListItem>();
    }
}
