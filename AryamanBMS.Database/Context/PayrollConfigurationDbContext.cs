using AryamanBMS.Models;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Database.Context
{
    public class PayrollConfigurationDbContext : DbContext
    {
        public PayrollConfigurationDbContext(
            DbContextOptions<PayrollConfigurationDbContext> options)
            : base(options)
        {
        }

        public DbSet<PayrollPolicyModel> PayrollPolicies { get; set; }

        public DbSet<PayrollPeriodLockModel> PayrollPeriodLocks { get; set; }

        public DbSet<ProfessionalTaxSlabModel> ProfessionalTaxSlabs { get; set; }

        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<PayrollPolicyModel>()
                .ToTable("TablePayrollPolicy");

            modelBuilder.Entity<PayrollPolicyModel>()
                .HasIndex(x => x.IsActive);

            modelBuilder.Entity<PayrollPeriodLockModel>()
                .ToTable("TablePayrollPeriodLock");

            modelBuilder.Entity<PayrollPeriodLockModel>()
                .HasIndex(x => new
                {
                    x.Month,
                    x.Year
                })
                .IsUnique();

            modelBuilder.Entity<ProfessionalTaxSlabModel>()
                .ToTable("TableProfessionalTaxSlab");

            modelBuilder.Entity<ProfessionalTaxSlabModel>()
                .HasIndex(x => new
                {
                    x.State,
                    x.IsActive
                });
        }
    }
}
