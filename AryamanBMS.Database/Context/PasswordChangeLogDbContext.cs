using AryamanBMS.Models;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Database.Context
{
    public class PasswordChangeLogDbContext : DbContext
    {
        public PasswordChangeLogDbContext(
            DbContextOptions<PasswordChangeLogDbContext> options)
            : base(options)
        {
        }

        public DbSet<PasswordChangeLogModel> PasswordChangeLogs { get; set; }

        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<PasswordChangeLogModel>()
                .ToTable("tablepasswordchangelogs");
        }
    }
}