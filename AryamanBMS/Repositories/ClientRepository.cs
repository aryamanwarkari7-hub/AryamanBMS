using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Repositories
{
    public class ClientRepository : IClientRepository
    {
        private readonly ApplicationDbContext _context;

        public ClientRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        private IQueryable<ClientModel> Clients =>
            _context.Clients.AsQueryable();

        public async Task<List<ClientModel>> GetAllAsync()
        {
            return await Clients
                .OrderBy(c => c.ClientName)
                .ToListAsync();
        }

        public async Task<ClientModel?> GetByIdAsync(int id)
        {
            return await Clients
                .FirstOrDefaultAsync(c => c.ClientId == id);
        }

        public async Task<bool> IsCodeUniqueAsync(string code, int excludeId = 0)
        {
            return !await _context.Clients
                .AnyAsync(c => c.ClientCode == code &&
                               c.ClientId != excludeId);
        }

        public async Task AddAsync(ClientModel client)
        {
            await _context.Clients.AddAsync(client);
        }

        public Task UpdateAsync(ClientModel client)
        {
            client.UpdatedOn = DateTime.Now;

            return Task.CompletedTask;
        }

        public Task DeleteAsync(ClientModel client)
        {
            _context.Clients.Remove(client);
            return Task.CompletedTask;
        }

        public async Task<bool> HasRelatedRecordsAsync(int clientId)
        {
            return await _context.Proposals.AnyAsync(p => p.ClientId == clientId)
                || await _context.PurchaseOrders.AnyAsync(o => o.ClientId == clientId);
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
