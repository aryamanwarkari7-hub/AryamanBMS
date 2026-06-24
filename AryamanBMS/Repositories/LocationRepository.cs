using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Repositories
{
    public class LocationRepository : ILocationRepository
    {
        private readonly ApplicationDbContext _context;

        public LocationRepository(
            ApplicationDbContext context)
        {
            _context = context;
        }

        public IQueryable<StateModel> States =>
            _context.States.AsNoTracking();

        public IQueryable<CityModel> Cities =>
            _context.Cities
                .Include(x => x.State)
                .AsNoTracking();

        public IQueryable<PincodeModel> Pincodes =>
            _context.Pincodes
                .Include(x => x.City)
                .AsNoTracking();

        public async Task<List<StateModel>>
            GetActiveStatesAsync()
        {
            return await _context.States
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.StateName)
                .ToListAsync();
        }

        public async Task<List<CityModel>>
            GetCitiesByStateAsync(int stateId)
        {
            return await _context.Cities
                .AsNoTracking()
                .Where(x =>
                    x.StateId == stateId &&
                    x.IsActive)
                .OrderBy(x => x.CityName)
                .ToListAsync();
        }

        public async Task<List<PincodeModel>>
            GetPincodesByCityAsync(int cityId)
        {
            return await _context.Pincodes
                .AsNoTracking()
                .Where(x =>
                    x.CityId == cityId &&
                    x.IsActive)
                .OrderBy(x => x.Pincode)
                .ThenBy(x => x.AreaName)
                .ToListAsync();
        }
    }
}