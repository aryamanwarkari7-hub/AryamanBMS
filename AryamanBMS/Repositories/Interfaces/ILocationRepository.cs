using AryamanBMS.Models;

namespace AryamanBMS.Repositories.Interfaces
{
    public interface ILocationRepository
    {
        IQueryable<StateModel> States { get; }

        IQueryable<CityModel> Cities { get; }

        IQueryable<PincodeModel> Pincodes { get; }

        Task<List<StateModel>> GetActiveStatesAsync();

        Task<List<CityModel>> GetCitiesByStateAsync(
            int stateId);

        Task<List<PincodeModel>> GetPincodesByCityAsync(
            int cityId);
    }
}