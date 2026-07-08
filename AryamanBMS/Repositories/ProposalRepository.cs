using AryamanBMS.Data;
using System.Data;
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
        .Include(p => p.Project)
        .Include(p => p.AuditTrail);

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

            return Task.CompletedTask;
        }

        public Task DeleteAsync(ProposalModel proposal)
        {
            if (proposal.IsConverted)
            {
                throw new InvalidOperationException(
                    "Converted proposals cannot be deleted.");
            }

            proposal.IsActive = false;
            proposal.UpdatedOn = DateTime.Now;

            return Task.CompletedTask;
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task CreateWithSequenceAsync(ProposalModel proposal)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            try
            {
                DateTime now = DateTime.Now;

                const string documentType = "Proposal";
                string sequencePeriod = now.ToString("yyyyMM");

                var sequence = await _context.FinancialSequences
                    .FirstOrDefaultAsync(x =>
                        x.DocumentType == documentType &&
                        x.FinancialYear == sequencePeriod);

                if (sequence == null)
                {
                    sequence = new FinancialSequenceModel
                    {
                        DocumentType = documentType,
                        FinancialYear = sequencePeriod,
                        LastNumber = 1,
                        UpdatedOn = now
                    };

                    await _context.FinancialSequences.AddAsync(sequence);
                }
                else
                {
                    sequence.LastNumber++;
                    sequence.UpdatedOn = now;
                }

                proposal.ProposalNumber =
                 $"PRO-{now:yyMM}-{sequence.LastNumber:0000}";

                proposal.CreatedOn = now;
                proposal.UpdatedOn = null;
                proposal.IsActive = true;

                await _context.Proposals.AddAsync(proposal);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<string> GenerateProposalNoAsync()
        {
            DateTime now = DateTime.Now;

            const string documentType = "Proposal";
            string sequencePeriod = now.ToString("yyyyMM");

            int lastNumber = await _context.FinancialSequences
                .Where(x =>
                    x.DocumentType == documentType &&
                    x.FinancialYear == sequencePeriod)
                .Select(x => x.LastNumber)
                .FirstOrDefaultAsync();

            return $"PRO-{now:yyMM}-{lastNumber + 1:0000}";
        }
    }
}
