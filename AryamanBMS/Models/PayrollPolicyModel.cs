using System.ComponentModel.DataAnnotations;

namespace AryamanBMS.Models
{
    public class PayrollPolicyModel
    {
        [Key]
        public int PayrollPolicyId { get; set; }

        [Required]
        [StringLength(100)]
        public string PolicyName { get; set; } = "Default Payroll Policy";

        [Required]
        [StringLength(30)]
        public string DivisorType { get; set; } = "Actual Calendar Days";

        public bool RequireAttendanceClosure { get; set; } = true;

        public bool RequireLeaveClosure { get; set; } = true;

        public bool ReleasePayslipAfterPaymentOnly { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        public DateTime? UpdatedOn { get; set; }
    }
}
