using AryamanBMS.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Database.Context
{
    public class NotificationDbContext
        : IdentityDbContext<ApplicationUserModel>
    {
        public NotificationDbContext(
            DbContextOptions<NotificationDbContext> options)
            : base(options)
        {
        }

        public DbSet<NotificationModel> TableNotification { get; set; }

        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<NotificationModel>(entity =>
            {
                entity.ToTable("TableNotification");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.Title)
                    .HasMaxLength(150)
                    .IsRequired();

                entity.Property(x => x.Message)
                    .HasMaxLength(500)
                    .IsRequired();

                entity.Property(x => x.NotificationType)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.ReferenceType)
                    .HasMaxLength(100);

                entity.Property(x => x.ActionUrl)
                    .HasMaxLength(500);

                entity.Property(x => x.IsRead)
                    .HasDefaultValue(false);

                entity.Property(x => x.CreatedOn)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(x => x.User)
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(x => new
                {
                    x.UserId,
                    x.IsRead,
                    x.CreatedOn
                });
            });
        }
    }
}