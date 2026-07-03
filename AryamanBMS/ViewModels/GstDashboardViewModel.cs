using AryamanBMS.Models;

namespace AryamanBMS.ViewModels
{
    public class GstDashboardViewModel
    {
        public int Month { get; set; }

        public int Year { get; set; }

        public string FinancialYear { get; set; } = "";

        public string SnapshotStatus { get; set; } = "Pending";

        public decimal SalesTaxable { get; set; }

        public decimal OutputGST { get; set; }

        public decimal InputGST { get; set; }

        public decimal NetGSTPayable { get; set; }

        public decimal InputCreditCarryForward { get; set; }

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
