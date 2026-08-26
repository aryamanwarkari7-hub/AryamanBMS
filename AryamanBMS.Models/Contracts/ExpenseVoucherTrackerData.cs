namespace AryamanBMS.Models;

public class ExpenseVoucherTrackerData
{
    public List<ExpenseVoucherModel> Vouchers { get; init; } = [];

    public int TotalPages { get; init; }
}
