using AryamanBMS.Models;

namespace AryamanBMS.Repositories.Interfaces
{
    public interface IClientRepository
    {
        Task<List<ClientModel>> GetAllAsync();
        Task<ClientModel?> GetByIdAsync(int id);
        Task<bool> IsCodeUniqueAsync(string code, int excludeId = 0);
        Task AddAsync(ClientModel client);
        Task UpdateAsync(ClientModel client);
        Task DeleteAsync(ClientModel client);
        Task<bool> HasRelatedRecordsAsync(int clientId);
        Task SaveAsync();
    }
}
