using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AryamanBMS.Models
{
    /// <summary>
    /// Tracks statutory/legal notices received from government bodies
    /// (GST, PF, ESIC, Income Tax, Labour, ROC, etc.) with reply/lifecycle tracking.
    /// </summary>
    public class NoticeModel
    {
        [Key]
        public int NoticeId { get; set; }

        [StringLength(100)]
        public string? NoticeNumber { get; set; }

        /// <summary>
        /// GST, PF, ESIC, IncomeTax, Labour, ROC, Other
        /// </summary>
        [Required]
        [StringLength(30)]
        public string Department { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        public DateTime NoticeDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime ReceivedDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DueDate { get; set; }

        [Required]
        [StringLength(255)]
        public string Subject { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? Description { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = FinancialConstants.NoticeStatus.Open;

        [DataType(DataType.Date)]
        public DateTime? ReplyDate { get; set; }

        [StringLength(2000)]
        public string? ReplyDetails { get; set; }

        [StringLength(500)]
        public string? Remarks { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        public DateTime? UpdatedOn { get; set; }

        [ValidateNever]
        public virtual ICollection<NoticeDocumentModel> Documents { get; set; } = new List<NoticeDocumentModel>();
    }
}
