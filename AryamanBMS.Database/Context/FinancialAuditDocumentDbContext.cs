using AryamanBMS.Models;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Database.Context
{
    public class FinancialAuditDocumentDbContext : DbContext
    {
        public FinancialAuditDocumentDbContext(
            DbContextOptions<FinancialAuditDocumentDbContext> options)
            : base(options)
        {
        }

        public DbSet<FinancialAuditDocumentModel>
            FinancialAuditDocuments
        { get; set; }

        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<FinancialAuditDocumentModel>()
                .ToTable("tablefinancialauditdocuments");

            modelBuilder.Entity<FinancialAuditDocumentModel>()
                .HasIndex(x => x.FinancialYear);

            modelBuilder.Entity<FinancialAuditDocumentModel>()
                .HasIndex(x => x.DocumentCategory);

            modelBuilder.Entity<FinancialAuditDocumentModel>()
                .HasIndex(x => x.IsActive);

            modelBuilder.Entity<FinancialAuditDocumentModel>()
                .HasIndex(x => x.IsFinalized);
        }
    }
}