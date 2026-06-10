using AryamanBMS.Models;

namespace AryamanBMS.Repositories.Interfaces;

    public interface IAttendanceRepository
    {
        IQueryable<AttendanceModel> Attendances { get; }

        Task<List<AttendanceModel>> GetAllAsync();

        Task<AttendanceModel?> GetByIdAsync(int id);

        Task AddAsync(AttendanceModel attendance);

        Task UpdateAsync(AttendanceModel attendance);

        Task DeleteAsync(AttendanceModel attendance);

        Task SaveAsync();
    }