using AryamanBMS.Models;

namespace AryamanBMS.Repositories.Interfaces
{
    public interface IProposalRepository
    {
        Task<List<ProposalModel>> GetAllAsync();
        Task<ProposalModel?> GetByIdAsync(int id);
        Task<List<ProposalModel>> GetByClientAsync(int clientId);
        Task<List<ProposalModel>> GetByStatusAsync(string status);
        Task AddAsync(ProposalModel proposal);
        Task UpdateAsync(ProposalModel proposal);
        Task DeleteAsync(ProposalModel proposal);
        Task SaveAsync();
    }
}
