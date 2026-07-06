using AryamanBMS.Models;

namespace AryamanBMS.Services.Interfaces
{
    public interface IProposalDocumentService
    {
        Task<ProposalDocumentVersionModel>
            GenerateAsync(
                ProposalModel proposal,
                string generatedByUserId);
    }
}