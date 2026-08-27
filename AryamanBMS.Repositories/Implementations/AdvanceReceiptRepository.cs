using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Repositories;

public class AdvanceReceiptRepository(ApplicationDbContext context) : IAdvanceReceiptRepository
{
    public Task<List<AdvanceReceiptModel>> GetAllAsync() => context.AdvanceReceipts.AsNoTracking().Include(x => x.Client).Include(x => x.Project).ToListAsync();
    public Task<bool> PaymentReferenceExistsAsync(string paymentReference) => context.AdvanceReceipts.AsNoTracking().AnyAsync(x => x.PaymentReference == paymentReference && !x.IsCancelled);
    public async Task CreateWithSequenceAsync(AdvanceReceiptModel receipt)
    {
        await using var transaction = await context.Database.BeginTransactionAsync();
        try { int count = await context.AdvanceReceipts.CountAsync(); receipt.AdvanceReceiptNo = $"ADV-{DateTime.Now:yyMM}-{count + 1:0000}"; await context.AdvanceReceipts.AddAsync(receipt); await context.SaveChangesAsync(); await transaction.CommitAsync(); }
        catch { await transaction.RollbackAsync(); throw; }
    }
    public Task<AdvanceReceiptModel?> GetAvailableByIdAsync(int id) => context.AdvanceReceipts.Include(x => x.Client).FirstOrDefaultAsync(x => x.AdvanceReceiptId == id && !x.IsCancelled);
    public Task<InvoiceModel?> GetIssuedInvoiceForClientAsync(int invoiceId, int clientId) => context.Invoices.FirstOrDefaultAsync(x => x.InvoiceId == invoiceId && x.ClientId == clientId && !x.IsDeleted && x.InvoiceStatus == "Issued");
    public Task<List<InvoiceModel>> GetOutstandingInvoicesForClientAsync(int clientId) => context.Invoices.AsNoTracking().Where(x => x.ClientId == clientId && !x.IsDeleted && x.InvoiceStatus == "Issued" && x.BalanceAmount > 0).OrderByDescending(x => x.InvoiceDate).ThenByDescending(x => x.InvoiceId).ToListAsync();
    public async Task SaveAdjustmentAsync()
    {
        await using var transaction = await context.Database.BeginTransactionAsync();
        try { await context.SaveChangesAsync(); await transaction.CommitAsync(); }
        catch { await transaction.RollbackAsync(); throw; }
    }
    public Task<List<ClientModel>> GetActiveClientsAsync() => context.Clients.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.ClientName).ToListAsync();
    public Task<List<ProjectModel>> GetActiveProjectsAsync() => context.Projects.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.ProjectName).ToListAsync();
}
