using AryamanBMS.Models;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Database.Context
{
    public class DailyWorkReportDbContext : DbContext
    {
        public DailyWorkReportDbContext(DbContextOptions<DailyWorkReportDbContext> options)
            : base(options) { }

        public DbSet<DailyWorkReportModel> DailyWorkReports { get; set; }
        public DbSet<DailyWorkReportDetailModel> DailyWorkReportDetails { get; set; }
        public DbSet<DailyWorkReportPlanModel> DailyWorkReportPlans { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<DailyWorkReportModel>().ToTable("tabledailyworkreport");

            modelBuilder
                .Entity<DailyWorkReportModel>()
                .HasIndex(x => new { x.EmployeeId, x.ReportDate })
                .IsUnique();

            modelBuilder.Entity<DailyWorkReportModel>().HasIndex(x => x.ReportDate);

            modelBuilder.Entity<DailyWorkReportDetailModel>().ToTable("tabledailyworkreportdetail");

            modelBuilder
                .Entity<DailyWorkReportDetailModel>()
                .HasOne(x => x.DailyWorkReport)
                .WithMany(x => x.WorkDetails)
                .HasForeignKey(x => x.DailyWorkReportId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DailyWorkReportPlanModel>().ToTable("tabledailyworkreportplan");

            modelBuilder
                .Entity<DailyWorkReportPlanModel>()
                .HasOne(x => x.DailyWorkReport)
                .WithMany(x => x.NextDayPlans)
                .HasForeignKey(x => x.DailyWorkReportId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
