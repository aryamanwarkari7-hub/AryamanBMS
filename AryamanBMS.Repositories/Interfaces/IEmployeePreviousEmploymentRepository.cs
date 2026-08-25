using AryamanBMS.Models;

namespace AryamanBMS.Repositories.Interfaces
{
    public interface IEmployeePreviousEmploymentRepository
    {
        IQueryable<EmployeePreviousEmploymentModel>
            PreviousEmployments
        { get; }

        Task AddAsync(
            EmployeePreviousEmploymentModel previousEmployment);

        Task UpdateAsync(
            EmployeePreviousEmploymentModel previousEmployment);

        Task DeleteAsync(
            EmployeePreviousEmploymentModel previousEmployment);

        Task SaveAsync();
    }
}