using AryamanBMS.Models;

namespace AryamanBMS.Repositories.Interfaces
{
    public interface IProjectTimelineRepository
    {
        IQueryable<ProjectTimelineModel> ProjectTimelines { get; }

        Task AddAsync(ProjectTimelineModel timeline);

        Task<ProjectTimelineModel?> GetByIdAsync(int id);

        Task SaveAsync();
    }
}