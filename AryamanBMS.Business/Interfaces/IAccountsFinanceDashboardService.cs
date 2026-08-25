using AryamanBMS.Models;

namespace AryamanBMS.Business.Interfaces;

public interface IAccountsFinanceDashboardService
{
    Task<AccountsFinanceDashboardData> GetAsync(int? month, int? year);
}
