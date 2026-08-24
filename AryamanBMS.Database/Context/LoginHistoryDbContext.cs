using AryamanBMS.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Database.Context
{
    public class LoginHistoryDbContext
        : IdentityDbContext<ApplicationUserModel>
    {
        public LoginHistoryDbContext(
            DbContextOptions<LoginHistoryDbContext> options)
            : base(options)
        {
        }

        public DbSet<LoginHistoryModel> TableLoginHistory { get; set; }

        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<LoginHistoryModel>(entity =>
            {
                entity.ToTable("TableLoginHistory");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.AttemptedUserName)
                    .HasMaxLength(256)
                    .IsRequired();

                entity.Property(x => x.EventType)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.FailureReason)
                    .HasMaxLength(250);

                entity.Property(x => x.IpAddress)
                    .HasMaxLength(45);

                entity.Property(x => x.UserAgent)
                    .HasMaxLength(500);

                entity.Property(x => x.OccurredOn)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(x => x.User)
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(x => x.UserId);
                entity.HasIndex(x => x.OccurredOn);
                entity.HasIndex(x => x.EventType);
            });
        }
    }
}