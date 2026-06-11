using AryamanBMS.Models;

namespace AryamanBMS.Repositories.Interfaces
{
    public interface ISalaryRecordRepository
    {
        IQueryable<SalaryRecordModel> SalaryRecords { get; }

        Task<SalaryRecordModel?> GetByIdAsync(int id);

        Task AddAsync(SalaryRecordModel salaryRecord);

        Task UpdateAsync(SalaryRecordModel salaryRecord);

        Task DeleteAsync(SalaryRecordModel salaryRecord);

        Task SaveAsync();
    }
}