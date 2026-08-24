using AryamanBMS.Models;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Database.Context
{
    public class AttendanceCalendarDbContext : DbContext
    {
        public AttendanceCalendarDbContext(
            DbContextOptions<AttendanceCalendarDbContext> options)
            : base(options)
        {
        }

        public DbSet<HolidayModel> Holidays { get; set; }
        public DbSet<WorkingDayOverrideModel> WorkingDayOverrides { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<HolidayModel>(entity =>
            {
                entity.ToTable("TableHoliday");

                entity.HasKey(x => x.HolidayId);

                entity.Property(x => x.HolidayName)
                    .IsRequired()
                    .HasMaxLength(160);

                entity.Property(x => x.MonthName)
                    .HasMaxLength(20);

                entity.Property(x => x.DayName)
                    .HasMaxLength(20);

                entity.Property(x => x.HolidayType)
                    .IsRequired()
                    .HasMaxLength(80)
                    .HasDefaultValue("Office Holiday");

                entity.HasIndex(x => x.HolidayDate)
                    .IsUnique();
            });

            modelBuilder.Entity<WorkingDayOverrideModel>(entity =>
            {
                entity.ToTable("TableWorkingDayOverride");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.OverrideType)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(x => x.Reason)
                    .HasMaxLength(250);

                entity.Property(x => x.CreatedByUserId)
                    .HasMaxLength(450);

                entity.HasIndex(x => x.OverrideDate)
                    .IsUnique();
            });
        }
    }
}