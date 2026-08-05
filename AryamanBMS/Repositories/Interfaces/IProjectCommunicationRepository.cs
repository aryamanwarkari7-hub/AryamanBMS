using AryamanBMS.Models;

namespace AryamanBMS.Repositories.Interfaces
{
    public interface IProjectCommunicationRepository
    {
        IQueryable<ProjectCommunicationModel> ProjectCommunications
        {
            get;
        }

        Task<List<ProjectCommunicationModel>>
            GetByProjectIdAsync(int projectId);

        Task<ProjectCommunicationModel?>
            GetByIdAsync(int id);

        Task AddAsync(
            ProjectCommunicationModel communication);

        Task UpdateAsync(
            ProjectCommunicationModel communication);

        Task DeleteAsync(
            ProjectCommunicationModel communication);

        

    }
}