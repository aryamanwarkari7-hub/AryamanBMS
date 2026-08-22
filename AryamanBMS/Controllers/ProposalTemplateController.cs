using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using AryamanBMS.Services.Interfaces;
using AryamanBMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Controllers
{
    [Authorize(Roles = "Admin,Master")]
    public class ProposalTemplateController
        : Controller
    {
        #region Actions

        private readonly IProposalTemplateRepository  _templateRepository;

        private readonly IFileStorageService _fileStorageService;

        private readonly  UserManager<ApplicationUserModel>   _userManager;

        private readonly ApplicationDbContext _context;

        public ProposalTemplateController(
          IProposalTemplateRepository templateRepository,
          IFileStorageService fileStorageService,
          UserManager<ApplicationUserModel> userManager,
          ApplicationDbContext context)
        {
            _templateRepository = templateRepository;
            _fileStorageService = fileStorageService;
            _userManager = userManager;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var templates =
                await _templateRepository
                    .GetAllAsync();

            return View(templates);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(
                new ProposalTemplateViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            ProposalTemplateViewModel vm)
        {
            if (vm.TemplateFile == null ||
                vm.TemplateFile.Length == 0)
            {
                ModelState.AddModelError(
                    nameof(vm.TemplateFile),
                    "Please select a Word template.");
            }
            else
            {
                string extension =
                    Path.GetExtension(
                            vm.TemplateFile.FileName)
                        .ToLowerInvariant();

                if (extension != ".docx")
                {
                    ModelState.AddModelError(
                        nameof(vm.TemplateFile),
                        "Only .docx proposal templates are allowed.");
                }
            }

            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            string? userId =
                _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Current user could not be identified.");

                return View(vm);
            }

            var upload =
                await _fileStorageService.UploadAsync(
                    vm.TemplateFile!,
                    "ProposalTemplates");

            if (!upload.Success)
            {
                ModelState.AddModelError(
                    nameof(vm.TemplateFile),
                    upload.ErrorMessage ??
                    "Template could not be uploaded.");

                return View(vm);
            }

            var template =
                new ProposalTemplateModel
                {
                    TemplateName =
                        vm.TemplateName.Trim(),

                    OriginalFileName =
                        upload.OriginalFileName,

                    StoredFilePath =
                        upload.RelativePath,

                    ContentType =
                        upload.ContentType,

                    FileSize =
                        upload.FileSize,

                    UploadedByUserId =
                        userId,

                    UploadedOn =
                        DateTime.Now,

                    IsActive =
                        true,

                    Remarks =
                        string.IsNullOrWhiteSpace(
                            vm.Remarks)
                            ? null
                            : vm.Remarks.Trim()
                };

            try
            {
                await _templateRepository
                    .AddNewVersionAsync(template);
            }
            catch
            {
                await _fileStorageService.DeleteAsync(
                    upload.RelativePath);

                throw;
            }

            TempData["Success"] =
                $"Proposal template version " +
                $"{template.VersionNumber} uploaded successfully.";

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Details(
            int id)
        {
            var template =
                await _templateRepository
                    .GetByIdAsync(id);

            if (template == null)
                return NotFound();

            return View(template);
        }
        [Authorize(Roles = "Admin,Master")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var template =
                await _templateRepository.GetByIdAsync(id);

            if (template == null)
                return NotFound();

            bool hasGeneratedDocuments =
                await _context.ProposalDocumentVersions
                    .AnyAsync(x =>
                        x.ProposalTemplateId == id);

            var vm =
                new ProposalTemplateEditViewModel
                {
                    ProposalTemplateId =
                        template.ProposalTemplateId,

                    TemplateName =
                        template.TemplateName,

                    VersionNumber =
                        template.VersionNumber,

                    Remarks =
                        template.Remarks,

                    IsActive =
                        template.IsActive,

                    CanEditVersion =
                        !hasGeneratedDocuments,

                    OriginalFileName =
                        template.OriginalFileName
                };

            return View(vm);
        }

        [Authorize(Roles = "Admin,Master")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProposalTemplateEditViewModel vm)
        {
            var template =
                await _templateRepository
                    .GetByIdAsync(
                        vm.ProposalTemplateId);

            if (template == null)
                return NotFound();

            bool hasGeneratedDocuments =
                await _context.ProposalDocumentVersions
                    .AnyAsync(x =>
                        x.ProposalTemplateId ==
                        vm.ProposalTemplateId);

            vm.CanEditVersion =
                !hasGeneratedDocuments;

            vm.OriginalFileName =
                template.OriginalFileName;

            vm.TemplateName =
                vm.TemplateName?.Trim() ??
                string.Empty;

            vm.Remarks =
                string.IsNullOrWhiteSpace(vm.Remarks)
                    ? null
                    : vm.Remarks.Trim();

            if (hasGeneratedDocuments)
            {
                vm.VersionNumber =
                    template.VersionNumber;
            }

            bool duplicateExists =
                await _context.ProposalTemplates
                    .AnyAsync(x =>
                        x.ProposalTemplateId !=
                            vm.ProposalTemplateId &&
                        x.TemplateName ==
                            vm.TemplateName &&
                        x.VersionNumber ==
                            vm.VersionNumber);

            if (duplicateExists)
            {
                ModelState.AddModelError(
                    nameof(vm.VersionNumber),
                    "A template with the same name and version already exists.");
            }

            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            template.TemplateName =
                vm.TemplateName;

            template.Remarks =
                vm.Remarks;

            template.IsActive =
                vm.IsActive;

            if (!hasGeneratedDocuments)
            {
                template.VersionNumber =
                    vm.VersionNumber;
            }

            await _templateRepository.SaveAsync();

            TempData["Success"] =
                "Proposal template updated successfully.";

            return RedirectToAction(
                nameof(Details),
                new
                {
                    id = template.ProposalTemplateId
                });
        }

        [HttpGet]
        public async Task<IActionResult> Download(
            int id)
        {
            var template =
                await _templateRepository
                    .GetByIdAsync(id);

            if (template == null)
                return NotFound();

            byte[]? fileBytes =
                await _fileStorageService
                    .DownloadAsync(
                        template.StoredFilePath);

            if (fileBytes == null)
                return NotFound();

            return File(
                fileBytes,
                template.ContentType,
                template.OriginalFileName);
        }

        [Authorize(Roles = "Admin,Master")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(
            int id)
        {
            var selected =
                await _templateRepository
                    .GetByIdAsync(id);

            if (selected == null)
                return NotFound();

            var templates =
                await _templateRepository
                    .GetAllAsync();

            foreach (var template in templates)
            {
                var tracked =
                    await _templateRepository
                        .GetByIdAsync(
                            template
                                .ProposalTemplateId);

                if (tracked != null)
                {
                    tracked.IsActive =
                        tracked.ProposalTemplateId ==
                        id;
                }
            }

            await _templateRepository.SaveAsync();

            TempData["Success"] =
                $"Template version " +
                $"{selected.VersionNumber} activated.";

            return RedirectToAction(nameof(Index));
        }
        #endregion
    }
}
