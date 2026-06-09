using AryamanBMS.Models;

namespace AryamanBMS.Repositories.Interfaces;

public interface IDesignationRepository
{
    IQueryable<DesignationModel> Designations { get; }

    Task<List<DesignationModel>> GetAllAsync();

    Task<DesignationModel?> GetByIdAsync(int id);

    Task AddAsync(DesignationModel designation);

    Task UpdateAsync(DesignationModel designation);

    Task DeleteAsync(DesignationModel designation);

    Task SaveAsync();
}