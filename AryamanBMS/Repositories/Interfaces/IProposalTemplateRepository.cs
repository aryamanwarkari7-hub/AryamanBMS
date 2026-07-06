using AryamanBMS.Models;

namespace AryamanBMS.Repositories.Interfaces
{
    public interface IProposalTemplateRepository
    {
        Task<List<ProposalTemplateModel>> GetAllAsync();

        Task<ProposalTemplateModel?> GetByIdAsync(int id);

        Task<ProposalTemplateModel?> GetActiveAsync();

        Task<int> GetNextVersionAsync(string templateName);

        Task AddNewVersionAsync(
            ProposalTemplateModel template);

        Task SaveAsync();
    }
}