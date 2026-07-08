using AryamanBMS.Models;

namespace AryamanBMS.ViewModels
{
    public class GstDashboardViewModel
    {
        public int Month { get; set; }

        public int Year { get; set; }

        public string FinancialYear { get; set; } = "";

        public string SnapshotStatus { get; set; } = "Pending";

        public string? FiledByUserId { get; set; }

        public DateTime? FiledOn { get; set; }

        public string? ReopenedByUserId { get; set; }

        public DateTime? ReopenedOn { get; set; }

        public string? ReopenReason { get; set; }

        public decimal SalesTaxable { get; set; }

        public decimal OutputGST { get; set; }

        public decimal InputGST { get; set; }

        public decimal NetGSTPayable { get; set; }

        public decimal InputCreditCarryForward { get; set; }

        public decimal PreviousITCCarryForward { get; set; }

        public decimal OutputCGSTBalance { get; set; }

        public decimal OutputSGSTBalance { get; set; }

        public decimal OutputIGSTBalance { get; set; }

        public decimal InputCGSTBalance { get; set; }

        public decimal InputSGSTBalance { get; set; }

        public decimal InputIGSTBalance { get; set; }

        public decimal ITCUtilizedForIGST { get; set; }

        public decimal ITCUtilizedForCGST { get; set; }

        public decimal ITCUtilizedForSGST { get; set; }

        public decimal CashPayableIGST { get; set; }

        public decimal CashPayableCGST { get; set; }

        public decimal CashPayableSGST { get; set; }

        public int InvoiceCount { get; set; }

        public int ExpenseVoucherCount { get; set; }

        public string Gstr1Status { get; set; } = "Pending";

        public string? Gstr1ArnNumber { get; set; }

        public DateTime? Gstr1FiledDate { get; set; }

        public string? Gstr1Remarks { get; set; }

        public string Gstr3BStatus { get; set; } = "Pending";

        public string? Gstr3BArnNumber { get; set; }

        public DateTime? Gstr3BFiledDate { get; set; }

        public string? Gstr3BRemarks { get; set; }

        public string ChallanStatus { get; set; } = "Pending";

        public string? ChallanNumber { get; set; }

        public decimal AmountPaid { get; set; }

        public DateTime? PaymentDate { get; set; }

        public string? PaymentMode { get; set; }

        public string? BankName { get; set; }

        public string? CPIN { get; set; }

        public string? CIN { get; set; }

        public string? ChallanRemarks { get; set; }

        public List<GstChallanModel> Challans { get; set; } = new();

        public List<GstDocumentModel> Documents { get; set; } = new();

        public int SnapshotId { get; set; }
    }
}
