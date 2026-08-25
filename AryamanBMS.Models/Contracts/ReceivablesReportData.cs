namespace AryamanBMS.Models;

public class ReceivablesReportData
{
    public decimal TotalInvoiced { get; init; }

    public decimal TotalCollected { get; init; }

    public decimal TotalOutstanding { get; init; }

    public decimal OverdueAmount { get; init; }

    public List<ClientReceivableData> ClientRows { get; init; } = [];

    public List<ProjectReceivableData> ProjectRows { get; init; } = [];

    public List<InvoiceReceivableData> InvoiceRows { get; init; } = [];
}

public class ClientReceivableData
{
    public int ClientId { get; init; }

    public string ClientName { get; init; } = string.Empty;

    public decimal TotalInvoiced { get; init; }

    public decimal TotalCollected { get; init; }

    public decimal Outstanding { get; init; }

    public decimal Overdue { get; init; }
}

public class ProjectReceivableData
{
    public int? ProjectId { get; init; }

    public string ProjectName { get; init; } = string.Empty;

    public decimal TotalInvoiced { get; init; }

    public decimal TotalCollected { get; init; }

    public decimal Outstanding { get; init; }

    public decimal Overdue { get; init; }
}

public class InvoiceReceivableData
{
    public int InvoiceId { get; init; }

    public string InvoiceNo { get; init; } = string.Empty;

    public string ClientName { get; init; } = string.Empty;

    public string ProjectName { get; init; } = string.Empty;

    public DateTime InvoiceDate { get; init; }

    public DateTime? DueDate { get; init; }

    public decimal InvoiceTotal { get; init; }

    public decimal AmountReceived { get; init; }

    public decimal OutstandingBalance { get; init; }

    public int AgeingDays { get; init; }

    public string PaymentStatus { get; init; } = string.Empty;
}

public class InvoiceAgeingReportData
{
    public decimal TotalOutstanding { get; init; }

    public decimal Bucket0To30 { get; init; }

    public decimal Bucket31To60 { get; init; }

    public decimal Bucket61To90 { get; init; }

    public decimal BucketAbove90 { get; init; }

    public List<InvoiceAgeingData> Rows { get; init; } = [];
}

public class InvoiceAgeingData
{
    public int InvoiceId { get; init; }

    public string InvoiceNo { get; init; } = string.Empty;

    public string ClientName { get; init; } = string.Empty;

    public DateTime InvoiceDate { get; init; }

    public DateTime? DueDate { get; init; }

    public decimal InvoiceTotal { get; init; }

    public decimal AmountReceived { get; init; }

    public decimal OutstandingBalance { get; init; }

    public int AgeingDays { get; init; }

    public string AgeingBucket { get; init; } = string.Empty;
}
