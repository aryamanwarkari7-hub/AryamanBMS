using AryamanBMS.Models;

namespace AryamanBMS.Repositories.Interfaces
{
    public interface ILocationRepository
    {

        IQueryable<CountryModel> Countries { get; }
        IQueryable<StateModel> States { get; }

        IQueryable<CityModel> Cities { get; }

        IQueryable<PincodeModel> Pincodes { get; }

        Task<List<CountryModel>> GetActiveCountriesAsync();

        Task<List<StateModel>> GetActiveStatesAsync();

        Task<List<CityModel>> GetCitiesByStateAsync(
            int stateId);

        Task<List<PincodeModel>> GetPincodesByCityAsync(
            int cityId);
    }
}