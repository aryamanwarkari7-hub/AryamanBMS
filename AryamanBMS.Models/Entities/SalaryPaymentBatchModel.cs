using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AryamanBMS.Models
{
    public class SalaryPaymentBatchModel
    {
        [Key]
        public int SalaryPaymentBatchId { get; set; }

        public int Month { get; set; }

        public int Year { get; set; }

        [StringLength(100)]
        public string? BankAccount { get; set; }

        public DateTime? PaymentDate { get; set; }

        public int TotalEmployees { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalNetSalary { get; set; }

        [StringLength(150)]
        public string? TransactionReference { get; set; }

        [StringLength(500)]
        public string? UploadedBankFilePath { get; set; }

        [StringLength(450)]
        public string? ProcessedByUserId { get; set; }

        [StringLength(30)]
        public string PaymentStatus { get; set; } = "Pending";

        [StringLength(500)]
        public string? FailureReason { get; set; }

        [StringLength(500)]
        public string? ReversalReason { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        public DateTime? UpdatedOn { get; set; }
    }
}
