using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AryamanBMS.Models
{
    public class OfficeAssetVerificationModel
    {
        [Key]
        public int OfficeAssetVerificationId { get; set; }

        public int OfficeAssetId { get; set; }

        [Required]
        public DateTime VerificationDate { get; set; } = DateTime.Today;

        [StringLength(50)]
        public string VerificationStatus { get; set; } = "Found";

        [StringLength(100)]
        public string? VerifiedLocation { get; set; }

        [StringLength(450)]
        public string VerifiedByUserId { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Remarks { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        [ForeignKey(nameof(OfficeAssetId))]
        [ValidateNever]
        public OfficeAssetModel? OfficeAsset { get; set; }
    }
}
