namespace AryamanBMS.Models;

public class PaymentReceiptTrackerData
{
    public List<PaymentReceiptModel> PaymentReceipts { get; init; } = [];

    public List<ClientModel> Clients { get; init; } = [];

    public PaymentReceiptSummaryData Summary { get; init; } = new();

    public int TotalPages { get; init; }
}

public class PaymentReceiptSummaryData
{
    public int TotalReceipts { get; init; }

    public decimal TotalReceived { get; init; }

    public int ActiveReceiptCount { get; init; }

    public int CancelledReceiptCount { get; init; }
}
