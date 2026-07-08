using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AryamanBMS.Models
{
    public class OfficeAssetMaintenanceModel
    {
        [Key]
        public int OfficeAssetMaintenanceId { get; set; }

        public int OfficeAssetId { get; set; }

        [Required]
        public DateTime MaintenanceDate { get; set; } = DateTime.Today;

        [StringLength(50)]
        public string MaintenanceType { get; set; } = "Repair";

        [StringLength(150)]
        public string? ServiceVendorName { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Cost { get; set; }

        [StringLength(500)]
        public string? IssueDescription { get; set; }

        [StringLength(500)]
        public string? Resolution { get; set; }

        [StringLength(30)]
        public string Status { get; set; } = "Completed";

        [StringLength(450)]
        public string? CreatedByUserId { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        [ForeignKey(nameof(OfficeAssetId))]
        [ValidateNever]
        public OfficeAssetModel? OfficeAsset { get; set; }
    }
}
