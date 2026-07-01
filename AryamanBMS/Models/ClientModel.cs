using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AryamanBMS.Models
{
    public class ClientModel
    {
        [Key]
        public int ClientId { get; set; }

        /// <summary>Auto-generated: CLT-0001, CLT-0002, …</summary>
        [StringLength(20)]
        public string ClientCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Client name is required.")]
        [StringLength(200)]
        public string ClientName { get; set; } = string.Empty;

        [StringLength(200)]
        public string? ContactPerson { get; set; }

        [StringLength(15)]
        [Phone]
        public string? Phone { get; set; }

        [StringLength(200)]
        [EmailAddress]
        public string? Email { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(100)]
        public string? State { get; set; }

        [StringLength(15)]
        public string? GSTNumber { get; set; }

        [StringLength(10)]
        public string? PANNumber { get; set; }

        /// <summary>e.g. "Government", "Private", "NGO", "Individual"</summary>
        [StringLength(50)]
        public string? ClientType { get; set; }

        public string? Remarks { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        public DateTime? UpdatedOn { get; set; }

        // Navigation
        [ValidateNever]
        public virtual ICollection<ProposalModel> Proposals { get; set; } = new List<ProposalModel>();

        [ValidateNever]
        public virtual ICollection<PurchaseOrderModel> PurchaseOrders { get; set; } = new List<PurchaseOrderModel>();
    }
}
