using AryamanBMS.Data;
using AryamanBMS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin,Finance,Sales")]
    public class ClientCommunicationController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ClientCommunicationController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int clientId)
        {
            var client = await _context.Clients
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ClientId == clientId);

            if (client == null)
                return NotFound();

            var communications = await _context.ClientCommunications
                .Include(x => x.Client)
                .Include(x => x.AssignedToEmployee)
                .Include(x => x.Proposal)
                .Include(x => x.Project)
                .Include(x => x.Invoice)
                .Where(x => x.ClientId == clientId)
                .OrderByDescending(x => x.CommunicationDate)
                .ToListAsync();

            ViewBag.Client = client;

            return View(communications);
        }

        public async Task<IActionResult> Create(int clientId)
        {
            var client = await _context.Clients
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ClientId == clientId);

            if (client == null)
                return NotFound();

            var model = new ClientCommunicationModel
            {
                ClientId = clientId,
                CommunicationDate = DateTime.Now,
                Direction = "Company",
                CommunicationType = "Call",
                Status = "Open"
            };

            await LoadLookups(clientId);

            ViewBag.Client = client;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ClientCommunicationModel model)
        {
            await LoadLookups(model.ClientId);

            var client = await _context.Clients
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ClientId == model.ClientId);

            if (client == null)
                return NotFound();

            ViewBag.Client = client;

            model.Subject = model.Subject?.Trim() ?? string.Empty;
            model.Summary = model.Summary?.Trim() ?? string.Empty;
            model.ActionItem = model.ActionItem?.Trim();
            model.CreatedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            model.CreatedOn = DateTime.Now;

            if (model.ProposalId.HasValue && !model.ProjectId.HasValue)
            {
                model.ProjectId = await _context.Proposals
                    .AsNoTracking()
                    .Where(x =>
                        x.ProposalId == model.ProposalId.Value &&
                        x.ClientId == model.ClientId)
                    .Select(x => x.ProjectId)
                    .FirstOrDefaultAsync();
            }

            if (!model.ActionRequired)
            {
                model.ActionItem = null;
                model.FollowUpDate = null;
                model.AssignedToEmployeeId = null;
            }

            if (!ModelState.IsValid)
                return View(model);

            _context.ClientCommunications.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Client communication recorded successfully.";

            return RedirectToAction(nameof(Index), new { clientId = model.ClientId });
        }

        public async Task<IActionResult> ProjectTimeline(int projectId)
        {
            var project = await _context.Projects
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == projectId);

            if (project == null)
                return NotFound();

            var communications = await _context.ClientCommunications
                .Include(x => x.Client)
                .Include(x => x.AssignedToEmployee)
                .Include(x => x.Proposal)
                .Include(x => x.Project)
                .Include(x => x.Invoice)
                .Where(x => x.ProjectId == projectId)
                .OrderByDescending(x => x.CommunicationDate)
                .ToListAsync();

            ViewBag.Project = project;

            return View(communications);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var model = await _context.ClientCommunications
                .FirstOrDefaultAsync(x => x.Id == id);

            if (model == null)
                return NotFound();

            var client = await _context.Clients
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ClientId == model.ClientId);

            if (client == null)
                return NotFound();

            await LoadLookups(model.ClientId);

            ViewBag.Client = client;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ClientCommunicationModel model, string? returnUrl)
        {
            await LoadLookups(model.ClientId);

            var client = await _context.Clients
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ClientId == model.ClientId);

            if (client == null)
                return NotFound();

            ViewBag.Client = client;

            model.Subject = model.Subject?.Trim() ?? string.Empty;
            model.Summary = model.Summary?.Trim() ?? string.Empty;
            model.ActionItem = model.ActionItem?.Trim();
            model.UpdatedOn = DateTime.Now;

            if (model.ProposalId.HasValue && !model.ProjectId.HasValue)
            {
                model.ProjectId = await _context.Proposals
                    .AsNoTracking()
                    .Where(x =>
                        x.ProposalId == model.ProposalId.Value &&
                        x.ClientId == model.ClientId)
                    .Select(x => x.ProjectId)
                    .FirstOrDefaultAsync();
            }

            if (!model.ActionRequired)
            {
                model.ActionItem = null;
                model.FollowUpDate = null;
                model.AssignedToEmployeeId = null;
            }

            if (!ModelState.IsValid)
                return View(model);

            var existing = await _context.ClientCommunications
                .FirstOrDefaultAsync(x => x.Id == model.Id);

            if (existing == null)
                return NotFound();

            existing.CommunicationDate = model.CommunicationDate;
            existing.Direction = model.Direction;
            existing.CommunicationType = model.CommunicationType;
            existing.Subject = model.Subject;
            existing.Summary = model.Summary;
            existing.ActionRequired = model.ActionRequired;
            existing.ActionItem = model.ActionItem;
            existing.FollowUpDate = model.FollowUpDate;
            existing.AssignedToEmployeeId = model.AssignedToEmployeeId;
            existing.ProposalId = model.ProposalId;
            existing.ProjectId = model.ProjectId;
            existing.InvoiceId = model.InvoiceId;
            existing.Status = model.Status;
            existing.UpdatedOn = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Client communication updated successfully.";

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Index), new { clientId = existing.ClientId });
        }

        public async Task<IActionResult> Details(int id, string? returnUrl)
        {
            var model = await _context.ClientCommunications
                .Include(x => x.Client)
                .Include(x => x.AssignedToEmployee)
                .Include(x => x.Proposal)
                .Include(x => x.Project)
                .Include(x => x.Invoice)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (model == null)
                return NotFound();

            ViewBag.ReturnUrl = returnUrl;

            return View(model);
        }

        private async Task LoadLookups(int clientId)
        {
            var employees = await _context.Employees
                 .AsNoTracking()
                 .Where(x => x.IsActive)
                 .ToListAsync();

            ViewBag.Employees = new SelectList(
                employees
                    .OrderBy(x => x.FullName)
                    .Select(x => new
                    {
                        EmployeeId = x.Id,
                        EmployeeName = $"{x.EmployeeCode} - {x.FullName}"
                    })
                    .ToList(),
                "EmployeeId",
                "EmployeeName");

            var proposals = await _context.Proposals
              .AsNoTracking()
              .Where(x => x.ClientId == clientId)
              .OrderByDescending(x => x.ProposalDate)
              .Select(x => new
              {
                  x.ProposalId,
                  ProposalDisplay = x.ProposalNumber + " - " + x.ProposalTitle,
                  x.ProjectId
              })
              .ToListAsync();

            ViewBag.Proposals = proposals;

            var clientProjects = await _context.Projects
                .AsNoTracking()
                .Where(x => x.ClientId == clientId && x.IsActive)
                .OrderBy(x => x.ProjectName)
                .Select(x => new
                {
                    x.Id,
                    ProjectDisplay = x.ProjectCode + " - " + x.ProjectName
                })
                .ToListAsync();

            ViewBag.Projects = new SelectList(
                clientProjects,
                "Id",
                "ProjectDisplay");

            ViewBag.Invoices = new SelectList(
                await _context.Invoices
                    .AsNoTracking()
                    .Where(x => x.ClientId == clientId)
                    .OrderByDescending(x => x.InvoiceDate)
                    .ToListAsync(),
                "InvoiceId",
                "InvoiceNo");
        }
    }
}