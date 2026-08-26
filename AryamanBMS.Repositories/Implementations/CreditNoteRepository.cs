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
}
