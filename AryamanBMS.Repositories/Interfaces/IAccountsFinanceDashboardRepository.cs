using AryamanBMS.Models;

namespace AryamanBMS.Repositories.Interfaces;

public interface IAccountsFinanceDashboardRepository
{
    Task<AccountsFinanceDashboardSnapshot> GetSnapshotAsync();
}
