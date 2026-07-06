using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using AryamanBMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin,Finance")]
    public class ClientController : Controller
    {
        private readonly IClientRepository _repository;
        private readonly ILocationRepository _locationRepository;

        public ClientController(
           IClientRepository repository,
           ILocationRepository locationRepository)
        {
            _repository = repository;
            _locationRepository = locationRepository;
        }

        #region Index

        public async Task<IActionResult> Index()
        {
            var clients = await _repository.GetAllAsync();
            return View(clients);
        }

        #endregion

        #region Create

        public async Task<IActionResult> Create()
        {
            await LoadLocationDropdownsAsync();

            return View(new ClientViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ClientViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                await LoadLocationDropdownsAsync();
                return View(vm);
            }

            vm.Client.ClientCode  = await GenerateClientCodeAsync();
            vm.Client.CreatedOn   = DateTime.Now;
            vm.Client.IsActive    = true;

            await _repository.AddAsync(vm.Client);
            await _repository.SaveAsync();

            TempData["Success"] = "Client created successfully.";
            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Edit

        public async Task<IActionResult> Edit(int id)
        {
            var client = await _repository.GetByIdAsync(id);

            if (client == null)
                return NotFound();

            await LoadLocationDropdownsAsync();

            return View(new ClientViewModel
            {
                Client = client
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ClientViewModel vm)
        {
            if (id != vm.Client.ClientId) return NotFound();

            if (!ModelState.IsValid)
            {
                await LoadLocationDropdownsAsync();
                return View(vm);
            }

            var existing = await _repository.GetByIdAsync(id);
            if (existing == null) return NotFound();

            existing.ClientName     = vm.Client.ClientName;
            existing.ContactPerson  = vm.Client.ContactPerson;
            existing.Phone          = vm.Client.Phone;
            existing.Email          = vm.Client.Email;
            existing.Address        = vm.Client.Address;
            existing.City           = vm.Client.City;
            existing.State          = vm.Client.State;
            existing.GSTNumber      = vm.Client.GSTNumber;
            existing.PANNumber      = vm.Client.PANNumber;
            existing.ClientType     = vm.Client.ClientType;
            existing.Remarks        = vm.Client.Remarks;
            existing.IsActive       = vm.Client.IsActive;

            await _repository.UpdateAsync(existing);
            await _repository.SaveAsync();

            TempData["Success"] = "Client updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Details

        public async Task<IActionResult> Details(int id)
        {
            var client = await _repository.GetByIdAsync(id);
            if (client == null) return NotFound();

            return View(client);
        }

        #endregion

        #region Delete

        public async Task<IActionResult> Delete(int id)
        {
            var client = await _repository.GetByIdAsync(id);
            if (client == null) return NotFound();

            return View(client);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var client = await _repository.GetByIdAsync(id);
            if (client == null) return NotFound();

            client.IsActive = !client.IsActive;
            client.UpdatedOn = DateTime.Now;

            await _repository.UpdateAsync(client);
            await _repository.SaveAsync();

            TempData["Success"] = client.IsActive
                ? "Client activated successfully."
                : "Client deactivated successfully.";

            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Toggle Status

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var client = await _repository.GetByIdAsync(id);
            if (client == null) return NotFound();

            client.IsActive   = !client.IsActive;
            client.UpdatedOn  = DateTime.Now;

            await _repository.UpdateAsync(client);
            await _repository.SaveAsync();

            return Ok();
        }

        #endregion

        #region API — used by dropdowns in Proposal / PO forms

        [HttpGet]
        public async Task<IActionResult> Get(int id)
        {
            var client = await _repository.GetByIdAsync(id);
            if (client == null) return NotFound();

            return Json(new
            {
                clientId      = client.ClientId,
                clientCode    = client.ClientCode,
                clientName    = client.ClientName,
                contactPerson = client.ContactPerson,
                phone         = client.Phone,
                email         = client.Email,
                gstNumber     = client.GSTNumber,
            });
        }

        #endregion

        #region Helpers

        private async Task<string> GenerateClientCodeAsync()
        {
            var clients = await _repository.GetAllAsync();

            int lastNumber = clients
                .Select(c => c.ClientCode)
                .Where(code => !string.IsNullOrWhiteSpace(code) &&
                               code.StartsWith("CLT-", StringComparison.OrdinalIgnoreCase))
                .Select(code => int.TryParse(
                    code.AsSpan(4),
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out int number)
                        ? number
                        : 0)
                .DefaultIfEmpty(0)
                .Max();

            return $"CLT-{(lastNumber + 1):D4}";
        }

        private async Task LoadLocationDropdownsAsync()
        {
            ViewBag.States =
                await _locationRepository.GetActiveStatesAsync();
        }
        #endregion
    }
}
