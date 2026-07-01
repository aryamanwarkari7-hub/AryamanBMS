using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Repositories
{
    public class InvoiceRepository : IInvoiceRepository
    {
        private readonly ApplicationDbContext _context;


        public InvoiceRepository(ApplicationDbContext context)
        {
            _context = context;
        }


        public IQueryable<InvoiceModel> Invoices => _context.Invoices;


        public async Task<List<InvoiceModel>> GetAllAsync()
        {
            return await Invoices

                .Include(i => i.Client)

                .Where(i => !i.IsDeleted)

                .OrderByDescending(i => i.InvoiceDate)

                .ToListAsync();
        }

        public async Task<InvoiceModel?> GetByIdAsync(int id)
        {
            return await Invoices

                .Include(i => i.Client)

                .Include(i => i.InvoiceDetails)

                .FirstOrDefaultAsync(i => i.InvoiceId == id);
        }

        public async Task<List<ClientModel>> GetClientsAsync()
        {
            return await _context.Clients

                .Where(c => c.IsActive)

                .OrderBy(c => c.ClientName)

                .ToListAsync();
        }

        public async Task AddAsync(InvoiceModel invoice)
        {
            invoice.CreatedOn = DateTime.Now;

            invoice.IsDeleted = false;


            if (invoice.PaidAmount < 0)
                invoice.PaidAmount = 0;


            invoice.BalanceAmount =
                invoice.GrandTotal - invoice.PaidAmount;



            await _context.Invoices.AddAsync(invoice);
        }

        public Task UpdateAsync(InvoiceModel invoice)
        {
            invoice.ModifiedOn = DateTime.Now;


            invoice.BalanceAmount =
                invoice.GrandTotal - invoice.PaidAmount;



            _context.Invoices.Update(invoice);


            return Task.CompletedTask;
        }


        public Task DeleteAsync(InvoiceModel invoice)
        {
            invoice.IsDeleted = true;

            invoice.ModifiedOn = DateTime.Now;


            _context.Invoices.Update(invoice);


            return Task.CompletedTask;
        }


        public async Task<string> GenerateInvoiceNoAsync()
        {
            string prefix = $"INV-{DateTime.Now:yyMM}-";


            int count = await _context.Invoices
                .CountAsync();



            return prefix +
                (count + 1)
                .ToString("0000");
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}