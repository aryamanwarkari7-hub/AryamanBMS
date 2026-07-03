using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Repositories
{
    public class GstConfigurationRepository : IGstConfigurationRepository
    {
        private readonly ApplicationDbContext _context;

        public GstConfigurationRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<GstConfigurationModel?> GetActiveAsync()
        {
            return await _context.GstConfigurations
                .AsNoTracking()
                .OrderByDescending(x => x.UpdatedOn)
                .FirstOrDefaultAsync(x => x.IsActive);
        }

        public async Task SaveActiveAsync(GstConfigurationModel configuration)
        {
            var activeConfigurations =
                await _context.GstConfigurations
                    .Where(x => x.IsActive)
                    .ToListAsync();

            foreach (var item in activeConfigurations)
            {
                if (item.GstConfigurationId != configuration.GstConfigurationId)
                {
                    item.IsActive = false;
                }
            }

            configuration.IsActive = true;
            configuration.UpdatedOn = DateTime.Now;

            if (configuration.GstConfigurationId == 0)
            {
                await _context.GstConfigurations.AddAsync(configuration);
            }
            else
            {
                var existing =
                    activeConfigurations.FirstOrDefault(x =>
                        x.GstConfigurationId == configuration.GstConfigurationId)
                    ?? await _context.GstConfigurations.FirstAsync(x =>
                        x.GstConfigurationId == configuration.GstConfigurationId);

                existing.CompanyName = configuration.CompanyName;
                existing.CompanyGstin = configuration.CompanyGstin;
                existing.RegisteredState = configuration.RegisteredState;
                existing.CgstRate = configuration.CgstRate;
                existing.SgstRate = configuration.SgstRate;
                existing.IgstRate = configuration.IgstRate;
                existing.IsActive = true;
                existing.UpdatedByUserId = configuration.UpdatedByUserId;
                existing.UpdatedOn = configuration.UpdatedOn;
            }

            await _context.SaveChangesAsync();
        }
    }
}
