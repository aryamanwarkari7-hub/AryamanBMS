using AryamanBMS.Models;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Database.Context
{
    public class NoticeDbContext : DbContext
    {
        public NoticeDbContext(
            DbContextOptions<NoticeDbContext> options)
            : base(options)
        {
        }

        public DbSet<NoticeModel> Notices { get; set; }

        public DbSet<NoticeDocumentModel> NoticeDocuments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<NoticeModel>()
                .ToTable("tablenotice");

            modelBuilder.Entity<NoticeModel>()
                .HasIndex(x => x.IsActive);

            modelBuilder.Entity<NoticeDocumentModel>()
                .ToTable("tablenoticedocument");

            modelBuilder.Entity<NoticeDocumentModel>()
                .HasIndex(x => x.IsActive);

            modelBuilder.Entity<NoticeDocumentModel>()
                .HasOne(x => x.Notice)
                .WithMany(x => x.Documents)
                .HasForeignKey(x => x.NoticeId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}