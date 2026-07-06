using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AryamanBMS.Models
{
    public class ProposalModel
    {
        [Key]
        public int ProposalId { get; set; }

        [StringLength(30)]
        public string ProposalNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Client is required.")]
        public int ClientId { get; set; }

        public int? ProjectId { get; set; }

        [Required(ErrorMessage = "Proposal title is required.")]
        [StringLength(300)]
        public string ProposalTitle { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        public DateTime ProposalDate { get; set; } = DateTime.Today;

        [DataType(DataType.Date)]
        public DateTime? ValidUntil { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Amount cannot be negative.")]
        public decimal? ProposalAmount { get; set; }

        [StringLength(10)]
        public string Currency { get; set; } = "INR";

        public string? Scope { get; set; }

        public string? Terms { get; set; }

        public string? Remarks { get; set; }

        [StringLength(10)]
        public string RevisionNumber { get; set; } = "00";

        [StringLength(150)]
        public string PreparedBy { get; set; } = string.Empty;

        [StringLength(150)]
        public string? PreparedByDesignation { get; set; }

        public string? ProblemStatement { get; set; }

        [StringLength(250)]
        public string? Timeline { get; set; }

        public string? TechnicalSolution { get; set; }

        public string? OutOfScope { get; set; }

        public string? CustomerResponsibilities { get; set; }

        public string? Deliverables { get; set; }

        public string? Dependencies { get; set; }

        public string? Assumptions { get; set; }

        public string? Risks { get; set; }

        public string? Warranty { get; set; }

        public string? CommercialDescription { get; set; }

        public string? PaymentTerms { get; set; }

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Draft";

        [ValidateNever] public string FileName { get; set; } = string.Empty;
        [ValidateNever] public string StoredFileName { get; set; } = string.Empty;
        [ValidateNever] public string? FileExtension { get; set; }
        [ValidateNever] public string? ContentType { get; set; }
        [ValidateNever] public long FileSize { get; set; }
        [ValidateNever] public string FilePath { get; set; } = string.Empty;
        [ValidateNever] public int VersionNo { get; set; } = 1;

        public bool IsConverted { get; set; } = false;
        public bool IsActive { get; set; } = true;

        public DateTime CreatedOn { get; set; } = DateTime.Now;
        public DateTime? UpdatedOn { get; set; }

        [ForeignKey(nameof(ClientId))]
        [ValidateNever]
        public virtual ClientModel? Client { get; set; }

        [ForeignKey(nameof(ProjectId))]
        [ValidateNever]
        public virtual ProjectModel? Project { get; set; }

        [ValidateNever]
        public virtual ICollection<PurchaseOrderModel> PurchaseOrders { get; set; }
            = new List<PurchaseOrderModel>();

        public int? ProposalTemplateId { get; set; }

        public ProposalTemplateModel? ProposalTemplate
        {
            get;
            set;
        }

        public ICollection<ProposalDocumentVersionModel>
            DocumentVersions
        { get; set; } =
                new List<ProposalDocumentVersionModel>();
    }
}