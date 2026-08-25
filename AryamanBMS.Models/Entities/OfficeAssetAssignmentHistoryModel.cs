using System.ComponentModel.DataAnnotations;

namespace AryamanBMS.Models
{
    public class OfficeAssetAssignmentHistoryModel
    {
        [Key]
        public int OfficeAssetAssignmentHistoryId { get; set; }

        public int OfficeAssetId { get; set; }

        public int EmployeeId { get; set; }

        public DateTime AssignedOn { get; set; } = DateTime.Now;

        public DateTime? ReturnedOn { get; set; }

        [StringLength(450)]
        public string AssignedByUserId { get; set; } = string.Empty;

        [StringLength(450)]
        public string? ReturnedByUserId { get; set; }

        [StringLength(200)]
        public string? ConditionOnAssignment { get; set; }

        [StringLength(200)]
        public string? ConditionOnReturn { get; set; }

        [StringLength(500)]
        public string? Remarks { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        public OfficeAssetModel OfficeAsset { get; set; } = null!;

        public EmployeeModel Employee { get; set; } = null!;
    }
}
