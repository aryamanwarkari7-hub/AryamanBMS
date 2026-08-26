using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
namespace AryamanBMS.Repositories;
public class DebitNoteRepository : IDebitNoteRepository
{
    private readonly ApplicationDbContext _context;
    public DebitNoteRepository(ApplicationDbContext context) => _context = context;
    public Task<List<DebitNoteModel>> GetAllAsync() => _context.DebitNotes.AsNoTracking().Include(x => x.OriginalInvoice).ThenInclude(x => x!.Client).ToListAsync();
    public Task<List<InvoiceModel>> GetIssuedInvoicesAsync() => _context.Invoices.AsNoTracking().Include(x => x.Client).Where(x => !x.IsDeleted && x.InvoiceStatus == "Issued").OrderByDescending(x => x.InvoiceDate).ToListAsync();
    public Task<InvoiceModel?> GetIssuedInvoiceAsync(int invoiceId) => _context.Invoices.FirstOrDefaultAsync(x => x.InvoiceId == invoiceId && !x.IsDeleted && x.InvoiceStatus == "Issued");
    public Task<int> GetDebitNoteCountAsync() => _context.DebitNotes.CountAsync();
    public async Task CreateWithInvoiceAdjustmentAsync(DebitNoteModel note)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            await _context.DebitNotes.AddAsync(note);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
