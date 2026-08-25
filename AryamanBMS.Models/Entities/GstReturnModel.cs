using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AryamanBMS.Models
{
    public class GstReturnModel
    {
        [Key]
        public int GstReturnId { get; set; }

        [Required]
        public int SnapshotId { get; set; }

        [ForeignKey(nameof(SnapshotId))]
        [ValidateNever]
        public virtual GstMonthlySnapshotModel? Snapshot { get; set; }


        /// <summary>
        /// GSTR1
        /// GSTR3B
        /// GSTR9
        /// </summary>
        [Required]
        [StringLength(20)]
        public string ReturnType { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending";

        [Column("ARNNumber")]
        [StringLength(100)]
        public string? ArnNumber { get; set; }

        [Column("FilingDate")]
        [DataType(DataType.Date)]
        public DateTime? FiledDate { get; set; }

        [StringLength(100)]
        public string? FiledBy { get; set; }

        [StringLength(450)]
        public string? FiledByUserId { get; set; }

        [StringLength(50)]
        public string? AcknowledgementNumber { get; set; }

        public string? Remarks { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        public DateTime? UpdatedOn { get; set; }

    }
}
