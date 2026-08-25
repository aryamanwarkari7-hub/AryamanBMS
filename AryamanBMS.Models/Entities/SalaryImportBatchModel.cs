using System.ComponentModel.DataAnnotations;

namespace AryamanBMS.Models
{
    public class SalaryImportBatchModel
    {
        [Key]
        public int SalaryImportBatchId { get; set; }

        [StringLength(250)]
        public string SourceFileName { get; set; } = string.Empty;

        [StringLength(450)]
        public string? ImportedByUserId { get; set; }

        public DateTime ImportedOn { get; set; } = DateTime.Now;

        public int Month { get; set; }

        public int Year { get; set; }

        public int TotalRows { get; set; }

        public int SuccessfulRows { get; set; }

        public int FailedRows { get; set; }

        public string? ErrorSummary { get; set; }

        public ICollection<SalaryRecordModel> SalaryRecords { get; set; }
            = new List<SalaryRecordModel>();
    }
}
