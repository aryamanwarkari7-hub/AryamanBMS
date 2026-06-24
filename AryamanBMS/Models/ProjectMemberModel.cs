using System.ComponentModel.DataAnnotations;

namespace AryamanBMS.Models
{
    public class ProjectMemberModel
    {
        public int Id { get; set; }

        [Required]
        public int ProjectId { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        [StringLength(100)]
        public string? RoleInProject { get; set; }

        public DateTime AssignedOn { get; set; } = DateTime.Now;

        public bool IsActive { get; set; } = true;

        public ProjectModel? Project { get; set; }

        public EmployeeModel? Employee { get; set; }
    }
}