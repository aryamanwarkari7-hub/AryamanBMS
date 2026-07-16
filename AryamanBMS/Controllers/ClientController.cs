using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using AryamanBMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Text.RegularExpressions;

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

        public async Task<IActionResult> Index(
    string? searchText,
    string sortBy = "ClientName",
    string sortOrder = "asc")
        {
            var clients = await _repository.GetAllAsync();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                string search = searchText.Trim();

                clients = clients
                    .Where(x =>
                        (!string.IsNullOrWhiteSpace(x.ClientCode) &&
                            x.ClientCode.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrWhiteSpace(x.ClientName) &&
                            x.ClientName.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrWhiteSpace(x.ContactPerson) &&
                            x.ContactPerson.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrWhiteSpace(x.Phone) &&
                            x.Phone.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrWhiteSpace(x.Email) &&
                            x.Email.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrWhiteSpace(x.GSTNumber) &&
                            x.GSTNumber.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrWhiteSpace(x.ClientType) &&
                            x.ClientType.Contains(search, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }

            bool desc =
                string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);

            clients = sortBy switch
            {
                "ClientCode" => desc
                    ? clients.OrderByDescending(x => x.ClientCode).ToList()
                    : clients.OrderBy(x => x.ClientCode).ToList(),

                "ContactPerson" => desc
                    ? clients.OrderByDescending(x => x.ContactPerson).ToList()
                    : clients.OrderBy(x => x.ContactPerson).ToList(),

                "ClientType" => desc
                    ? clients.OrderByDescending(x => x.ClientType).ToList()
                    : clients.OrderBy(x => x.ClientType).ToList(),

                "Status" => desc
                    ? clients.OrderByDescending(x => x.IsActive).ToList()
                    : clients.OrderBy(x => x.IsActive).ToList(),

                _ => desc
                    ? clients.OrderByDescending(x => x.ClientName).ToList()
                    : clients.OrderBy(x => x.ClientName).ToList()
            };

            ViewBag.SearchText = searchText;
            ViewBag.SortBy = sortBy;
            ViewBag.SortOrder = sortOrder;

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

            NormalizeClientGstDetails(vm.Client);
            ValidateClientGstDetails(vm.Client);

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

            NormalizeClientGstDetails(vm.Client);
            ValidateClientGstDetails(vm.Client);

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
            existing.StateCode = vm.Client.StateCode;
            existing.RegistrationType = vm.Client.RegistrationType;
            existing.PlaceOfSupply = vm.Client.PlaceOfSupply;
            existing.PlaceOfSupplyStateCode = vm.Client.PlaceOfSupplyStateCode;
            existing.CreditPeriod = vm.Client.CreditPeriod;
            existing.PaymentTerms = vm.Client.PaymentTerms;
            existing.BillingAddress = vm.Client.BillingAddress;
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
                clientId = client.ClientId,
                clientCode = client.ClientCode,
                clientName = client.ClientName,
                contactPerson = client.ContactPerson,
                phone = client.Phone,
                email = client.Email,
                gstNumber = client.GSTNumber,
                state = client.State,
                stateCode = client.StateCode,
                registrationType = client.RegistrationType,
                billingAddress = client.BillingAddress,
                placeOfSupply = client.PlaceOfSupply,
                placeOfSupplyStateCode = client.PlaceOfSupplyStateCode,
                creditPeriod = client.CreditPeriod,
                paymentTerms = client.PaymentTerms
            });
        }

        #endregion

        #region Helpers

        private static readonly Regex GstinRegex =
    new(@"^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z][1-9A-Z]Z[0-9A-Z]$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex PanRegex =
            new(@"^[A-Z]{5}[0-9]{4}[A-Z]$",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private void NormalizeClientGstDetails(ClientModel client)
        {
            client.GSTNumber =
                string.IsNullOrWhiteSpace(client.GSTNumber)
                    ? null
                    : client.GSTNumber.Trim().ToUpperInvariant();

            client.PANNumber =
                string.IsNullOrWhiteSpace(client.PANNumber)
                    ? null
                    : client.PANNumber.Trim().ToUpperInvariant();

            client.StateCode =
                NormalizeStateCode(client.StateCode);

            client.PlaceOfSupplyStateCode =
                NormalizeStateCode(client.PlaceOfSupplyStateCode);

            client.RegistrationType =
                string.IsNullOrWhiteSpace(client.RegistrationType)
                    ? "Unregistered"
                    : client.RegistrationType.Trim();

            client.PaymentTerms =
                string.IsNullOrWhiteSpace(client.PaymentTerms)
                    ? null
                    : client.PaymentTerms.Trim();

            client.BillingAddress =
                string.IsNullOrWhiteSpace(client.BillingAddress)
                    ? client.Address
                    : client.BillingAddress.Trim();

            if (!string.IsNullOrWhiteSpace(client.GSTNumber) &&
                client.GSTNumber.Length >= 2)
            {
                client.StateCode =
                    NormalizeStateCode(client.GSTNumber[..2]);
            }

            if (string.IsNullOrWhiteSpace(client.PlaceOfSupply))
            {
                client.PlaceOfSupply = client.State;
            }

            if (string.IsNullOrWhiteSpace(client.PlaceOfSupplyStateCode))
            {
                client.PlaceOfSupplyStateCode = client.StateCode;
            }
        }

        private void ValidateClientGstDetails(ClientModel client)
        {
            bool isRegistered =
                client.RegistrationType == "Regular" ||
                client.RegistrationType == "Composition" ||
                client.RegistrationType == "SEZ";

            if (isRegistered &&
                string.IsNullOrWhiteSpace(client.GSTNumber))
            {
                ModelState.AddModelError(
                    "Client.GSTNumber",
                    "GSTIN is required for registered clients.");
            }

            if (!string.IsNullOrWhiteSpace(client.GSTNumber) &&
                !GstinRegex.IsMatch(client.GSTNumber))
            {
                ModelState.AddModelError(
                    "Client.GSTNumber",
                    "Enter a valid 15-character GSTIN.");
            }

            if (!string.IsNullOrWhiteSpace(client.PANNumber) &&
                !PanRegex.IsMatch(client.PANNumber))
            {
                ModelState.AddModelError(
                    "Client.PANNumber",
                    "Enter a valid PAN.");
            }

            if (!string.IsNullOrWhiteSpace(client.GSTNumber) &&
                !string.IsNullOrWhiteSpace(client.PANNumber) &&
                client.GSTNumber.Length >= 12 &&
                client.GSTNumber.Substring(2, 10) != client.PANNumber)
            {
                ModelState.AddModelError(
                    "Client.PANNumber",
                    "PAN must match characters 3 to 12 of GSTIN.");
            }

            if (!string.IsNullOrWhiteSpace(client.StateCode) &&
                !string.IsNullOrWhiteSpace(client.GSTNumber) &&
                client.GSTNumber.Length >= 2 &&
                client.StateCode != client.GSTNumber[..2])
            {
                ModelState.AddModelError(
                    "Client.StateCode",
                    "State code must match the first two digits of GSTIN.");
            }
        }

        private static string? NormalizeStateCode(string? stateCode)
        {
            stateCode = stateCode?.Trim();

            if (string.IsNullOrWhiteSpace(stateCode))
            {
                return null;
            }

            return stateCode.PadLeft(2, '0')[..2];
        }

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
