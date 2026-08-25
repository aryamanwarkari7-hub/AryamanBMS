using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AryamanBMS.Models
{
    public class OfficeAssetModel
    {
        [Key]
        public int OfficeAssetId { get; set; }

        [Required]
        [StringLength(200)]
        public string AssetName { get; set; } = string.Empty;

        /// <summary>
        /// Laptop, Furniture, AC, Projector, Printer, Vehicle, Other
        /// </summary>
        [Required]
        [StringLength(50)]
        public string AssetCategory { get; set; } = string.Empty;

        [StringLength(100)]
        public string? AssetCode { get; set; }

        [StringLength(100)]
        public string? SerialNumber { get; set; }

        [StringLength(100)]
        public string? ModelNumber { get; set; }

        [StringLength(100)]
        public string? Manufacturer { get; set; }

        [StringLength(100)]
        public string? Brand { get; set; }

        [StringLength(1000)]
        public string? ConfigurationDetails { get; set; }

        [StringLength(100)]
        public string? Barcode { get; set; }

        [Required]
        public DateTime PurchaseDate { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PurchaseValue { get; set; }

        [StringLength(200)]
        public string? VendorName { get; set; }

        public int? VendorId { get; set; }

        public int? ExpenseVoucherId { get; set; }

        public int? PurchaseOrderId { get; set; }

        [StringLength(100)]
        public string? VendorInvoiceNumber { get; set; }

        public DateTime? VendorInvoiceDate { get; set; }

        [StringLength(100)]
        public string? LocationName { get; set; }

        [StringLength(100)]
        public string? Building { get; set; }

        [StringLength(100)]
        public string? Floor { get; set; }

        [StringLength(100)]
        public string? RoomOrSeat { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxableAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CGSTAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SGSTAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal IGSTAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalGSTAmount { get; set; }

        public bool ITCEligible { get; set; }

        [StringLength(50)]
        public string ITCStatus { get; set; } = "Not Applicable";

        [Column(TypeName = "decimal(18,2)")]
        public decimal CapitalizedValue { get; set; }

        public bool IsCapitalized { get; set; }

        public DateTime? CapitalizedOn { get; set; }

        [StringLength(450)]
        public string? CapitalizedByUserId { get; set; }

        [StringLength(200)]
        public string? AssignedTo { get; set; }

        public int? AssignedEmployeeId { get; set; }

        [Required]
        [StringLength(10)]
        public string FinancialYear { get; set; } = string.Empty;

        /// <summary>
        /// InUse, Idle, UnderRepair, Disposed
        /// </summary>
        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "InUse";

        public DateTime? WarrantyStartDate { get; set; }

        public DateTime? WarrantyEndDate { get; set; }

        public bool HasAmc { get; set; }

        [StringLength(150)]
        public string? AmcVendorName { get; set; }

        public DateTime? AmcStartDate { get; set; }

        public DateTime? AmcEndDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DepreciationRate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal AccumulatedDepreciation { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal WrittenDownValue { get; set; }

        public DateTime? DisposalDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DisposalValue { get; set; }

        [StringLength(500)]
        public string? DisposalReason { get; set; }

        public DateTime? LostOrDamagedOn { get; set; }

        [StringLength(500)]
        public string? LostOrDamagedReason { get; set; }

        [StringLength(450)]
        public string? LastVerifiedByUserId { get; set; }

        public DateTime? LastVerifiedOn { get; set; }

        [StringLength(50)]
        public string? LastVerificationStatus { get; set; }

        [StringLength(450)]
        public string? ArchivedByUserId { get; set; }

        public DateTime? ArchivedOn { get; set; }

        [StringLength(500)]
        public string? ArchiveReason { get; set; }

        public bool IsActive { get; set; } = true;

        [StringLength(500)]
        public string? Remarks { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        public DateTime? UpdatedOn { get; set; }

        public EmployeeModel? AssignedEmployee { get; set; }

        public VendorModel? Vendor { get; set; }

        public ExpenseVoucherModel? ExpenseVoucher { get; set; }

        public PurchaseOrderModel? PurchaseOrder { get; set; }

        public ICollection<OfficeAssetAssignmentHistoryModel> AssignmentHistory
        { get; set; } = new List<OfficeAssetAssignmentHistoryModel>();

        public ICollection<OfficeAssetDocumentModel> Documents { get; set; }
            = new List<OfficeAssetDocumentModel>();

        public ICollection<OfficeAssetMaintenanceModel> MaintenanceHistory { get; set; }
            = new List<OfficeAssetMaintenanceModel>();

        public ICollection<OfficeAssetVerificationModel> VerificationHistory { get; set; }
            = new List<OfficeAssetVerificationModel>();
    }
}
