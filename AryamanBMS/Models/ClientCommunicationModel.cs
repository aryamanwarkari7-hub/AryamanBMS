using System.ComponentModel.DataAnnotations;

namespace AryamanBMS.Models
{
    public class ClientCommunicationModel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ClientId { get; set; }

        public ClientModel? Client { get; set; }

        [Required]
        [DataType(DataType.DateTime)]
        public DateTime CommunicationDate { get; set; } = DateTime.Now;

        [Required]
        [StringLength(20)]
        public string Direction { get; set; } = "Company";
        // Client, Company, Internal

        [Required]
        [StringLength(50)]
        public string CommunicationType { get; set; } = "Call";
        // Call, Email, WhatsApp, Meeting, Site Visit, Payment Follow-up, Other

        [Required]
        [StringLength(200)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        [StringLength(2000)]
        public string Summary { get; set; } = string.Empty;

        public bool ActionRequired { get; set; }

        [StringLength(500)]
        public string? ActionItem { get; set; }

        [DataType(DataType.Date)]
        public DateTime? FollowUpDate { get; set; }

        public int? AssignedToEmployeeId { get; set; }

        public EmployeeModel? AssignedToEmployee { get; set; }

        public int? ProposalId { get; set; }

        public ProposalModel? Proposal { get; set; }

        public int? ProjectId { get; set; }

        public ProjectModel? Project { get; set; }

        public int? InvoiceId { get; set; }

        public InvoiceModel? Invoice { get; set; }

        [StringLength(30)]
        public string Status { get; set; } = "Open";
        // Open, Follow-up, Closed


        [Display(Name = "Share with Project Team")]
        public bool ShareWithProjectTeam { get; set; }

        [StringLength(200)]
        [Display(Name = "Project Communication Subject")]
        public string? ProjectSubject { get; set; }

        [StringLength(5000)]
        [Display(Name = "Project Communication Summary")]

        public string? ProjectSummary { get; set; }

        [StringLength(450)]
        public string? CreatedByUserId { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        public DateTime? UpdatedOn { get; set; }
    }
}