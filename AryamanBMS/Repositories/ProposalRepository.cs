using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Repositories
{
    public class ProposalRepository : IProposalRepository
    {
        private readonly ApplicationDbContext _context;

        public ProposalRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        private IQueryable<ProposalModel> Proposals =>
            _context.Proposals
                .Include(p => p.Client)
                .Include(p => p.Project);

        public async Task<List<ProposalModel>> GetAllAsync()
        {
            return await Proposals
                .OrderByDescending(p => p.ProposalDate)
                .ThenBy(p => p.ProposalNumber)
                .ToListAsync();
        }

        public async Task<ProposalModel?> GetByIdAsync(int id)
        {
            return await Proposals
                .FirstOrDefaultAsync(p => p.ProposalId == id);
        }

        public async Task<List<ProposalModel>> GetByClientAsync(int clientId)
        {
            return await Proposals
                .Where(p => p.ClientId == clientId)
                .OrderByDescending(p => p.ProposalDate)
                .ToListAsync();
        }

        public async Task<List<ProposalModel>> GetByStatusAsync(string status)
        {
            return await Proposals
                .Where(p => p.Status == status)
                .OrderByDescending(p => p.ProposalDate)
                .ToListAsync();
        }

        public async Task AddAsync(ProposalModel proposal)
        {
            await _context.Proposals.AddAsync(proposal);
        }

        public Task UpdateAsync(ProposalModel proposal)
        {
            proposal.UpdatedOn = DateTime.Now;
            _context.Proposals.Update(proposal);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(ProposalModel proposal)
        {
            _context.Proposals.Remove(proposal);
            return Task.CompletedTask;
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
