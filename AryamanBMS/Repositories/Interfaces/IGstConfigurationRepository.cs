using AryamanBMS.Models;

namespace AryamanBMS.Repositories.Interfaces
{
    public interface IGstConfigurationRepository
    {
        Task<GstConfigurationModel?> GetActiveAsync();

        Task SaveActiveAsync(GstConfigurationModel configuration);
    }
}
