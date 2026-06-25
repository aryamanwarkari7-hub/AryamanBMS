using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AryamanBMS.Models
{
    [Table("tablecompoffcredit")]
    public class CompOffCreditModel
    {
        public int Id { get; set; }

        public int EmployeeId { get; set; }

        [Required]
        public DateTime WorkedDate { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal CreditDays { get; set; } = 1.00m;

        [Required]
        public DateTime ExpiryDate { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending";

        public int? AttendanceId { get; set; }

        public DateTime RequestedOn { get; set; } = DateTime.Now;

        [StringLength(255)]
        public string? RequestedBy { get; set; }

        public DateTime? ApprovedOn { get; set; }

        [StringLength(255)]
        public string? ApprovedBy { get; set; }

        public DateTime? RejectedOn { get; set; }

        [StringLength(255)]
        public string? RejectedBy { get; set; }

        [StringLength(500)]
        public string? Remarks { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        public DateTime? UpdatedOn { get; set; }

        [ForeignKey(nameof(EmployeeId))]
        public EmployeeModel? Employee { get; set; }

        [ForeignKey(nameof(AttendanceId))]
        public AttendanceModel? Attendance { get; set; }
    }
}