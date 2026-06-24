using AryamanBMS.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AryamanBMS.Controllers
{
    [Authorize]
    public class LocationController : Controller
    {
        private readonly ILocationRepository _locationRepository;

        public LocationController(
            ILocationRepository locationRepository)
        {
            _locationRepository = locationRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetStates()
        {
            var states =
                await _locationRepository
                    .GetActiveStatesAsync();

            var data = states.Select(x => new
            {
                id = x.Id,
                name = x.StateName
            });

            return Json(data);
        }

        [HttpGet]
        public async Task<IActionResult> GetCities(
            int stateId)
        {
            if (stateId <= 0)
            {
                return Json(Array.Empty<object>());
            }

            var cities =
                await _locationRepository
                    .GetCitiesByStateAsync(stateId);

            var data = cities.Select(x => new
            {
                id = x.Id,
                name = x.CityName
            });

            return Json(data);
        }

        [HttpGet]
        public async Task<IActionResult> GetPincodes(
            int cityId)
        {
            if (cityId <= 0)
            {
                return Json(Array.Empty<object>());
            }

            var pincodes =
                await _locationRepository
                    .GetPincodesByCityAsync(cityId);

            var data = pincodes.Select(x => new
            {
                value = x.Pincode,

                text = string.IsNullOrWhiteSpace(
                    x.AreaName)
                    ? x.Pincode
                    : $"{x.Pincode} - {x.AreaName}"
            });

            return Json(data);
        }
    }
}