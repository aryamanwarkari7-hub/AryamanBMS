using AryamanBMS.Business.Interfaces;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
namespace AryamanBMS.Business.Services;
public class AdvanceReceiptQueryService(IAdvanceReceiptRepository repository) : IAdvanceReceiptQueryService
{
    public async Task<List<AdvanceReceiptModel>> GetAllAsync(string? search, string sortBy, string sortOrder)
    {
        var receipts = await repository.GetAllAsync();
        if (!string.IsNullOrWhiteSpace(search)) { var k = search.Trim().ToLower(); receipts = receipts.Where(x => x.AdvanceReceiptNo.ToLower().Contains(k) || x.PaymentMode.ToLower().Contains(k) || (x.PaymentReference?.ToLower().Contains(k) ?? false) || (x.Client?.ClientName.ToLower().Contains(k) ?? false) || (x.Project?.ProjectName.ToLower().Contains(k) ?? false)).ToList(); }
        bool d = string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);
        return sortBy switch { "ReceiptNo" => d ? receipts.OrderByDescending(x => x.AdvanceReceiptNo).ToList() : receipts.OrderBy(x => x.AdvanceReceiptNo).ToList(), "Client" => d ? receipts.OrderByDescending(x => x.Client!.ClientName).ToList() : receipts.OrderBy(x => x.Client!.ClientName).ToList(), "Project" => d ? receipts.OrderByDescending(x => x.Project!.ProjectName).ToList() : receipts.OrderBy(x => x.Project!.ProjectName).ToList(), "Amount" => d ? receipts.OrderByDescending(x => x.Amount).ToList() : receipts.OrderBy(x => x.Amount).ToList(), "AvailableBalance" => d ? receipts.OrderByDescending(x => x.AvailableBalance).ToList() : receipts.OrderBy(x => x.AvailableBalance).ToList(), "PaymentMode" => d ? receipts.OrderByDescending(x => x.PaymentMode).ToList() : receipts.OrderBy(x => x.PaymentMode).ToList(), _ => d ? receipts.OrderByDescending(x => x.ReceiptDate).ThenByDescending(x => x.AdvanceReceiptId).ToList() : receipts.OrderBy(x => x.ReceiptDate).ThenBy(x => x.AdvanceReceiptId).ToList() };
    }
}
