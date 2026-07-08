using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AryamanBMS.Models
{
    public class BillingMilestoneModel
    {
        [Key]
        public int BillingMilestoneId { get; set; }

        public int PurchaseWorkOrderId { get; set; }

        public int? ProjectId { get; set; }

        [Required]
        [StringLength(200)]
        public string MilestoneName { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? MilestoneDescription { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal MilestoneValue { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal BilledValue { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal RemainingBillableValue { get; set; }

        [Required]
        [StringLength(30)]
        public string CompletionStatus { get; set; } = "Pending";

        public DateTime? ApprovalDate { get; set; }

        public int SortOrder { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        public DateTime? UpdatedOn { get; set; }

        [ForeignKey(nameof(PurchaseWorkOrderId))]
        [ValidateNever]
        public virtual PurchaseOrderModel? PurchaseWorkOrder { get; set; }

        [ForeignKey(nameof(ProjectId))]
        [ValidateNever]
        public virtual ProjectModel? Project { get; set; }
    }
}