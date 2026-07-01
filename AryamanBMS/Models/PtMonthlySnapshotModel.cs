using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AryamanBMS.Models
{
    /// <summary>
    /// Monthly aggregate Professional Tax liability snapshot, computed from SalaryRecordModel.
    /// PT is employee-only (no employer contribution), unlike PF/ESIC.
    /// </summary>
    public class PtMonthlySnapshotModel
    {
        [Key]
        public int PtSnapshotId { get; set; }

        [Required]
        public int Month { get; set; }

        [Required]
        public int Year { get; set; }

        [Required]
        [StringLength(10)]
        public string FinancialYear { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPayable { get; set; }

        public int EmployeeCount { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = FinancialConstants.StatutoryStatus.Pending;

        public DateTime GeneratedOn { get; set; } = DateTime.Now;

        [ValidateNever]
        public virtual ICollection<PtChallanModel> Challans { get; set; } = new List<PtChallanModel>();

        [ValidateNever]
        public virtual ICollection<PtDocumentModel> Documents { get; set; } = new List<PtDocumentModel>();
    }
}