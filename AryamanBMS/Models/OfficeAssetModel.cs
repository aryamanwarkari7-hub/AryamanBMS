using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AryamanBMS.Models
{
    public class OfficeAssetModel
    {
        [Key]
        public int OfficeAssetId { get; set; }

        [Required]
        [StringLength(200)]
        public string AssetName { get; set; } = string.Empty;

        /// <summary>
        /// Laptop, Furniture, AC, Projector, Printer, Vehicle, Other
        /// </summary>
        [Required]
        [StringLength(50)]
        public string AssetCategory { get; set; } = string.Empty;

        [StringLength(100)]
        public string? AssetCode { get; set; }

        [Required]
        public DateTime PurchaseDate { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PurchaseValue { get; set; }

        [StringLength(200)]
        public string? VendorName { get; set; }

        [StringLength(200)]
        public string? AssignedTo { get; set; }

        [Required]
        [StringLength(10)]
        public string FinancialYear { get; set; } = string.Empty;

        /// <summary>
        /// InUse, Idle, UnderRepair, Disposed
        /// </summary>
        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "InUse";

        [StringLength(500)]
        public string? Remarks { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        public DateTime? UpdatedOn { get; set; }
    }
}