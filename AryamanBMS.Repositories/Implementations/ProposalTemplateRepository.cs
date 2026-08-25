using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace AryamanBMS.Repositories
{
    public class ProposalTemplateRepository
        : IProposalTemplateRepository
    {
        private readonly ApplicationDbContext _context;

        public ProposalTemplateRepository(
            ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<ProposalTemplateModel>>
            GetAllAsync()
        {
            return await _context.ProposalTemplates
                .AsNoTracking()
                .OrderByDescending(x => x.VersionNumber)
                .ThenByDescending(x => x.UploadedOn)
                .ToListAsync();
        }

        public async Task<ProposalTemplateModel?>
            GetByIdAsync(int id)
        {
            return await _context.ProposalTemplates
                .FirstOrDefaultAsync(x =>
                    x.ProposalTemplateId == id);
        }

        public async Task<ProposalTemplateModel?>
            GetActiveAsync()
        {
            return await _context.ProposalTemplates
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderByDescending(x => x.VersionNumber)
                .FirstOrDefaultAsync();
        }

        public async Task<int> GetNextVersionAsync(
            string templateName)
        {
            int latestVersion =
                await _context.ProposalTemplates
                    .Where(x =>
                        x.TemplateName == templateName)
                    .Select(x => (int?)x.VersionNumber)
                    .MaxAsync() ?? 0;

            return latestVersion + 1;
        }

        public async Task AddNewVersionAsync(
            ProposalTemplateModel template)
        {
            await using var transaction =
                await _context.Database
                    .BeginTransactionAsync(
                        IsolationLevel.Serializable);

            try
            {
                //var activeTemplates =
                //    await _context.ProposalTemplates
                //        .Where(x => x.IsActive)
                //        .ToListAsync();

                //foreach (var activeTemplate in
                //         activeTemplates)
                //{
                //    activeTemplate.IsActive = false;
                //}

                int latestVersion =
                    await _context.ProposalTemplates
                        .Where(x =>
                            x.TemplateName ==
                            template.TemplateName)
                        .Select(x =>
                            (int?)x.VersionNumber)
                        .MaxAsync() ?? 0;

                template.VersionNumber =
                    latestVersion + 1;

                template.IsActive = true;
                template.UploadedOn = DateTime.Now;

                await _context.ProposalTemplates
                    .AddAsync(template);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}