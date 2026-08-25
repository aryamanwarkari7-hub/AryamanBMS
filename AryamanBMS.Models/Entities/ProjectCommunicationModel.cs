using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AryamanBMS.Models
{
    public class ProjectCommunicationModel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProjectId { get; set; }

        [ForeignKey(nameof(ProjectId))]
        public ProjectModel? Project { get; set; }

        
        public int? CreatedByEmployeeId { get; set; }

        [ForeignKey(nameof(CreatedByEmployeeId))]
        public EmployeeModel? CreatedByEmployee { get; set; }

        [Required]
        [StringLength(50)]
        public string CommunicationType { get; set; } = "Internal";
        // Internal, Call, Email, Teams, WhatsApp, Meeting, Site Visit, Other

        [Required]
        [StringLength(200)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        [StringLength(5000)]
        public string Message { get; set; } = string.Empty;

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Open";
        // Open, Information, Action Required, Closed

        public int? ClientCommunicationId { get; set; }

        [ForeignKey(nameof(ClientCommunicationId))]
        public ClientCommunicationModel? ClientCommunication { get; set; }

        [StringLength(450)]
        public string? CreatedByUserId { get; set; }

        [ForeignKey(nameof(CreatedByUserId))]
        public ApplicationUserModel? CreatedByUser { get; set; }

        public bool IsSystemGenerated { get; set; } = false;

        public bool IsEdited { get; set; } = false;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        public DateTime? UpdatedOn { get; set; }
    }
}