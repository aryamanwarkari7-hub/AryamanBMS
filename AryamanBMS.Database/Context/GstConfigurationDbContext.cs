using AryamanBMS.Models;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Database.Context
{
    public class GstConfigurationDbContext : DbContext
    {
        public GstConfigurationDbContext(
            DbContextOptions<GstConfigurationDbContext> options)
            : base(options)
        {
        }

        public DbSet<GstConfigurationModel> GstConfigurations
        {
            get;
            set;
        }

        public DbSet<GstLutDocumentModel> GstLutDocuments
        {
            get;
            set;
        }

        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<GstConfigurationModel>()
                .ToTable("tablegstconfiguration");

            modelBuilder.Entity<GstConfigurationModel>()
                .HasIndex(x => x.IsActive);

            modelBuilder.Entity<GstLutDocumentModel>(entity =>
{
    entity.ToTable("TableGstLutDocument");

    entity.HasKey(x => x.GstLutDocumentId);

    entity.HasIndex(x => new
    {
        x.GstConfigurationId,
        x.IsActive
    });
});
        }
    }
}