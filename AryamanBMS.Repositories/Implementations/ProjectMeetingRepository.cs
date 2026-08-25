using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Repositories
{
    public class ProjectMeetingRepository
        : IProjectMeetingRepository
    {
        private readonly ApplicationDbContext _context;

        public ProjectMeetingRepository(
            ApplicationDbContext context)
        {
            _context = context;
        }

        public IQueryable<ProjectMeetingModel> Meetings =>
            _context.ProjectMeetings
                .Include(m => m.Project)
                .Include(m => m.Attendees)
                .Include(m => m.ActionItems);

        public async Task<ProjectMeetingModel?> GetByIdAsync(int id)
        {
            return await _context.ProjectMeetings
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<ProjectMeetingModel?> GetDetailsAsync(int id)
        {
            return await _context.ProjectMeetings
                .Include(m => m.Project)
                .Include(m => m.Attendees)
                    .ThenInclude(a => a.Employee)
                .Include(m => m.ActionItems)
                    .ThenInclude(a => a.AssignedEmployee)
                .AsSplitQuery()
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<List<ProjectModel>> GetActiveProjectsAsync()
        {
            return await _context.Projects
                .Where(p => p.IsActive)
                .OrderBy(p => p.ProjectName)
                .ToListAsync();
        }

        public async Task<List<EmployeeModel>> GetActiveEmployeesAsync()
        {
            return await _context.Employees
                .Where(e => e.IsActive)
                .OrderBy(e => e.FirstName)
                .ThenBy(e => e.LastName)
                .ToListAsync();
        }

        public async Task AddAsync(ProjectMeetingModel meeting)
        {
            await _context.ProjectMeetings.AddAsync(meeting);
        }

        public Task UpdateAsync(ProjectMeetingModel meeting)
        {
            _context.ProjectMeetings.Update(meeting);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(ProjectMeetingModel meeting)
        {
            _context.ProjectMeetings.Remove(meeting);
            return Task.CompletedTask;
        }

        public async Task<ProjectMeetingAttendeeModel?> GetAttendeeAsync(int id)
        {
            return await _context.ProjectMeetingAttendees
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task AddAttendeeAsync(
            ProjectMeetingAttendeeModel attendee)
        {
            await _context.ProjectMeetingAttendees.AddAsync(attendee);
        }

        public Task DeleteAttendeeAsync(
            ProjectMeetingAttendeeModel attendee)
        {
            _context.ProjectMeetingAttendees.Remove(attendee);
            return Task.CompletedTask;
        }

        public async Task<ProjectMeetingActionItemModel?> GetActionItemAsync(
            int id)
        {
            return await _context.ProjectMeetingActionItems
                .Include(a => a.Meeting)
                .Include(a => a.AssignedEmployee)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task AddActionItemAsync(
            ProjectMeetingActionItemModel actionItem)
        {
            await _context.ProjectMeetingActionItems.AddAsync(actionItem);
        }

        public Task UpdateActionItemAsync(
            ProjectMeetingActionItemModel actionItem)
        {
            _context.ProjectMeetingActionItems.Update(actionItem);
            return Task.CompletedTask;
        }

        public Task DeleteActionItemAsync(
            ProjectMeetingActionItemModel actionItem)
        {
            _context.ProjectMeetingActionItems.Remove(actionItem);
            return Task.CompletedTask;
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}