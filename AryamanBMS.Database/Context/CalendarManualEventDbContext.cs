using AryamanBMS.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Database.Context
{
    public class CalendarManualEventDbContext
        : IdentityDbContext<ApplicationUserModel>
    {
        public CalendarManualEventDbContext(
            DbContextOptions<CalendarManualEventDbContext> options)
            : base(options)
        {
        }

        public DbSet<CalendarManualEventModel>
            CalendarManualEvents { get; set; }

        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<CalendarManualEventModel>()
                .ToTable("TableCalendarManualEvent");

            modelBuilder.Entity<CalendarManualEventModel>()
                .HasOne(x => x.CreatedByUser)
                .WithMany()
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<CalendarManualEventModel>()
                .HasOne(x => x.UpdatedByUser)
                .WithMany()
                .HasForeignKey(x => x.UpdatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}