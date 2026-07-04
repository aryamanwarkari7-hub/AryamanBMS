using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AryamanBMS.Models
{
    /// <summary>
    /// Monthly aggregate PF liability snapshot, computed from SalaryRecordModel.
    /// </summary>
    public class PfMonthlySnapshotModel
    {
        [Key]
        public int PfSnapshotId { get; set; }

        [Required]
        public int Month { get; set; }

        [Required]
        public int Year { get; set; }

        [Required]
        [StringLength(10)]
        public string FinancialYear { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal EmployeeDeductionTotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal EmployerContributionTotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPayable { get; set; }

        public int EmployeeCount { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = FinancialConstants.StatutoryStatus.Pending;

        public DateTime GeneratedOn { get; set; } = DateTime.Now;

        [StringLength(450)]
        public string? FiledByUserId { get; set; }

        public DateTime? FiledOn { get; set; }

        [StringLength(450)]
        public string? PaidByUserId { get; set; }

        public DateTime? PaidOn { get; set; }

        [ValidateNever]
        public virtual ICollection<PfChallanModel> Challans { get; set; } = new List<PfChallanModel>();

        [ValidateNever]
        public virtual ICollection<PfDocumentModel> Documents { get; set; } = new List<PfDocumentModel>();
    }
}