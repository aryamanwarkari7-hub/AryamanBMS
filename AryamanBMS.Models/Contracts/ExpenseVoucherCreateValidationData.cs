namespace AryamanBMS.Models;

public class ExpenseVoucherCreateValidationData
{
    public ExpenseCategoryModel? Category { get; init; }

    public IReadOnlyDictionary<string, string> Errors { get; init; } =
        new Dictionary<string, string>();
}
