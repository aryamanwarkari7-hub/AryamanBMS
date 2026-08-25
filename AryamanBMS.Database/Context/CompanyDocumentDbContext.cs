using AryamanBMS.Models;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Database.Context
{
    public class CompanyDocumentDbContext : DbContext
    {
        public CompanyDocumentDbContext(
            DbContextOptions<CompanyDocumentDbContext> options)
            : base(options)
        {
        }

        public DbSet<CompanyDocumentCategoryModel>
            CompanyDocumentCategories { get; set; }

        public DbSet<CompanyDocumentModel>
            CompanyDocuments { get; set; }

        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<CompanyDocumentCategoryModel>()
                .ToTable("tablecompanydocumentcategory");

            modelBuilder.Entity<CompanyDocumentCategoryModel>()
                .HasIndex(x => x.CategoryName)
                .IsUnique();

            modelBuilder.Entity<CompanyDocumentModel>()
                .ToTable("tablecompanydocument");

            modelBuilder.Entity<CompanyDocumentModel>()
                .HasIndex(x => x.IsActive);

            modelBuilder.Entity<CompanyDocumentModel>()
                .HasOne(x => x.Category)
                .WithMany()
                .HasForeignKey(x => x.DocumentCategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}