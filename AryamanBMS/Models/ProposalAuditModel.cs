using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AryamanBMS.Models
{
    public class ProposalAuditModel
    {
        [Key]
        public int ProposalAuditId { get; set; }

        public int ProposalId { get; set; }

        [Required]
        [StringLength(50)]
        public string ActionType { get; set; } = string.Empty;

        [StringLength(30)]
        public string? OldStatus { get; set; }

        [StringLength(30)]
        public string? NewStatus { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? OldAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? NewAmount { get; set; }

        [StringLength(10)]
        public string? OldRevisionNumber { get; set; }

        [StringLength(10)]
        public string? NewRevisionNumber { get; set; }

        [StringLength(1000)]
        public string? Remarks { get; set; }

        [StringLength(450)]
        public string? ChangedByUserId { get; set; }

        public DateTime ChangedOn { get; set; } = DateTime.Now;

        [ForeignKey(nameof(ProposalId))]
        [ValidateNever]
        public virtual ProposalModel? Proposal { get; set; }
    }
}