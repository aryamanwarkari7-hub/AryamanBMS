namespace AryamanBMS.Models;

public class PurchaseReportData
{
    public List<PurchaseReportRow> VendorPayable { get; init; } = [];

    public List<PurchaseReportRow> CategoryWise { get; init; } = [];

    public List<PurchaseReportRow> VendorWise { get; init; } = [];

    public List<PurchaseReportRow> ProjectWise { get; init; } = [];

    public List<PurchaseReportRow> DepartmentWise { get; init; } = [];

    public List<PurchaseReportRow> Reimbursements { get; init; } = [];

    public List<PurchaseReportRow> Itc { get; init; } = [];

    public List<PurchaseReportRow> PaidUnpaid { get; init; } = [];

    public List<PurchaseReportRow> Monthly { get; init; } = [];

    public List<PurchaseReportRow> Capital { get; init; } = [];
}

public class PurchaseReportRow
{
    public string Label { get; init; } = string.Empty;

    public int Count { get; init; }

    public decimal TaxableAmount { get; init; }

    public decimal GstAmount { get; init; }

    public decimal TotalAmount { get; init; }

    public decimal PaidAmount { get; init; }

    public decimal BalanceAmount { get; init; }
}
