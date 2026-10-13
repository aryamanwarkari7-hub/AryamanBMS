using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AryamanBMS.Models;

public class ProjectCommunicationDocumentModel
{
    public int Id { get; set; }

    [Required]
    public int ProjectCommunicationId { get; set; }

    [ForeignKey(nameof(ProjectCommunicationId))]
    public ProjectCommunicationModel? ProjectCommunication { get; set; }

    [Required]
    [StringLength(255)]
    public string OriginalFileName { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string DocumentName { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string StoredFileName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string ContentType { get; set; } = string.Empty;

    public long FileSize { get; set; }

    public DateTime UploadedOn { get; set; } = DateTime.Now;

    [StringLength(450)]
    public string? UploadedByUserId { get; set; }

    public bool IsActive { get; set; } = true;
}
