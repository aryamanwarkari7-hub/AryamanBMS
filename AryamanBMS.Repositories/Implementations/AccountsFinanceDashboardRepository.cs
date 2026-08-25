using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Repositories;

public class AccountsFinanceDashboardRepository
    : IAccountsFinanceDashboardRepository
{
    private readonly ApplicationDbContext _context;

    public AccountsFinanceDashboardRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AccountsFinanceDashboardSnapshot> GetSnapshotAsync()
    {
        var invoices = await _context.Invoices
            .AsNoTracking()
            .Include(x => x.Client)
            .Where(x => !x.IsDeleted)
            .ToListAsync();

        var receipts = await _context.PaymentReceipts
            .AsNoTracking()
            .Include(x => x.Client)
            .Include(x => x.Invoice)
            .Where(x => x.IsActive && !x.IsCancelled)
            .ToListAsync();

        var advances = await _context.AdvanceReceipts
            .AsNoTracking()
            .Include(x => x.Client)
            .Where(x => !x.IsCancelled)
            .ToListAsync();

        var expenses = await _context.ExpenseVouchers
            .AsNoTracking()
            .Include(x => x.Category)
            .Where(x => x.IsActive && !x.IsReversed)
            .ToListAsync();

        var creditNotes = await _context.CreditNotes
            .AsNoTracking()
            .Where(x => !x.IsCancelled)
            .ToListAsync();

        var debitNotes = await _context.DebitNotes
            .AsNoTracking()
            .Where(x => !x.IsCancelled)
            .ToListAsync();

        var assets = await _context.OfficeAssets
            .AsNoTracking()
            .Where(x => x.IsActive)
            .ToListAsync();

        return new AccountsFinanceDashboardSnapshot
        {
            Invoices = invoices,
            Receipts = receipts,
            Advances = advances,
            Expenses = expenses,
            CreditNotes = creditNotes,
            DebitNotes = debitNotes,
            Assets = assets
        };
    }
}
