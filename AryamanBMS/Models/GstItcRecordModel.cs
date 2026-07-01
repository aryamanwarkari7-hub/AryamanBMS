
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AryamanBMS.Models
{
    /// <summary>
    /// Stores Input Tax Credit (ITC) records.
    /// </summary>
    public class GstItcRecordModel
    {
        [Key]
        public int ItcRecordId { get; set; }

        [Required]
        public int SnapshotId { get; set; }

        [ForeignKey(nameof(SnapshotId))]
        [ValidateNever]
        public virtual GstMonthlySnapshotModel? Snapshot { get; set; }

        [Required]
        [StringLength(200)]
        public string VendorName { get; set; } = string.Empty;

        [StringLength(15)]
        public string? VendorGSTIN { get; set; }

        [Required]
        [StringLength(100)]
        public string InvoiceNumber { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        public DateTime InvoiceDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxableAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CGST { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SGST { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal IGST { get; set; }

        [Required]
        [StringLength(20)]
        public string EligibilityStatus { get; set; } = "Eligible";

        [StringLength(500)]
        public string? Remarks { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        public DateTime? UpdatedOn { get; set; }
    }
}

