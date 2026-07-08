using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AryamanBMS.Models
{
    public class SalaryAdvanceModel
    {
        [Key]
        public int SalaryAdvanceId { get; set; }

        public int EmployeeId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal AdvanceAmount { get; set; }

        public DateTime AdvanceDate { get; set; } = DateTime.Today;

        [StringLength(450)]
        public string? ApprovedByUserId { get; set; }

        public DateTime? ApprovedOn { get; set; }

        public int RecoveryStartMonth { get; set; }

        public int RecoveryStartYear { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal MonthlyRecoveryAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalRecovered { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal OutstandingBalance { get; set; }

        [StringLength(30)]
        public string Status { get; set; } = "Open";

        [StringLength(500)]
        public string? Remarks { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        public DateTime? UpdatedOn { get; set; }

        public EmployeeModel? Employee { get; set; }
    }
}
