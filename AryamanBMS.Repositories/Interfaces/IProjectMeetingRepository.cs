using AryamanBMS.Models;

namespace AryamanBMS.Repositories.Interfaces
{
    public interface IProjectMeetingRepository
    {
        IQueryable<ProjectMeetingModel> Meetings { get; }

        Task<ProjectMeetingModel?> GetByIdAsync(int id);

        Task<ProjectMeetingModel?> GetDetailsAsync(int id);

        Task<List<ProjectModel>> GetActiveProjectsAsync();

        Task<List<EmployeeModel>> GetActiveEmployeesAsync();

        Task AddAsync(ProjectMeetingModel meeting);

        Task UpdateAsync(ProjectMeetingModel meeting);

        Task DeleteAsync(ProjectMeetingModel meeting);


        Task<ProjectMeetingAttendeeModel?> GetAttendeeAsync(int id);

        Task AddAttendeeAsync(ProjectMeetingAttendeeModel attendee);

        Task DeleteAttendeeAsync(ProjectMeetingAttendeeModel attendee);


        Task<ProjectMeetingActionItemModel?> GetActionItemAsync(int id);

        Task AddActionItemAsync(ProjectMeetingActionItemModel actionItem);

        Task UpdateActionItemAsync(ProjectMeetingActionItemModel actionItem);

        Task DeleteActionItemAsync(ProjectMeetingActionItemModel actionItem);


        Task SaveAsync();
    }
}