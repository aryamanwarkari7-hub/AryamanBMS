using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Repositories;

public class CreditNoteRepository : ICreditNoteRepository
{
    private readonly ApplicationDbContext _context;

    public CreditNoteRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<CreditNoteModel>> GetAllAsync()
    {
        return await _context.CreditNotes
            .AsNoTracking()
            .Include(x => x.OriginalInvoice)
            .ThenInclude(x => x!.Client)
            .ToListAsync();
    }

    public async Task<List<InvoiceModel>> GetIssuedInvoicesAsync()
    {
        return await _context.Invoices
            .AsNoTracking()
            .Include(x => x.Client)
            .Where(x => !x.IsDeleted && x.InvoiceStatus == "Issued")
            .OrderByDescending(x => x.InvoiceDate)
            .ToListAsync();
    }

    public Task<InvoiceModel?> GetIssuedInvoiceAsync(int invoiceId) => _context.Invoices.FirstOrDefaultAsync(x => x.InvoiceId == invoiceId && !x.IsDeleted && x.InvoiceStatus == "Issued");

    public async Task CreateWithInvoiceAdjustmentAsync(CreditNoteModel note, InvoiceModel invoice)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try { int count = await _context.CreditNotes.CountAsync(); note.CreditNoteNo = $"CRN-{DateTime.Now:yyMM}-{count + 1:0000}"; await _context.CreditNotes.AddAsync(note); await _context.SaveChangesAsync(); await transaction.CommitAsync(); }
        catch { await transaction.RollbackAsync(); throw; }
    }
}
