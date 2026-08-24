using AryamanBMS.Models;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Database.Context
{
    public class CompanyProfileDbContext : DbContext
    {
        public CompanyProfileDbContext(
            DbContextOptions<CompanyProfileDbContext> options)
            : base(options)
        {
        }

        public DbSet<CompanyProfileModel> CompanyProfiles { get; set; }

        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<CompanyProfileModel>()
                .ToTable("tablecompanyprofile");

            modelBuilder.Entity<CompanyProfileModel>()
                .HasIndex(x => x.GSTIN)
                .IsUnique();
        }
    }
}