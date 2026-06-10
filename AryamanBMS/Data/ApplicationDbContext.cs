using AryamanBMS.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Data
{
    public class ApplicationDbContext: IdentityDbContext<ApplicationUserModel>
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<DepartmentModel> Departments { get; set; }

        public DbSet<DesignationModel> Designations { get; set; }

        public DbSet<EmployeeModel> Employees { get; set; }

        public DbSet<AttendanceModel> Attendances { get; set; }


        // Leave
        public DbSet<LeaveTypeModel> LeaveTypes { get; set; }

        public DbSet<LeaveApplicationModel> LeaveApplications { get; set; }

        public DbSet<LeaveBalanceModel> LeaveBalances { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<DepartmentModel>()
                .ToTable("TableDepartment");

            modelBuilder.Entity<DesignationModel>()
                .ToTable("TableDesignation");

            modelBuilder.Entity<EmployeeModel>()
                .ToTable("TableEmployee");

            modelBuilder.Entity<EmployeeModel>()
                .HasOne(e => e.ApplicationUser)
                .WithMany()
                .HasForeignKey(e => e.ApplicationUserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<AttendanceModel>()
                .ToTable("TableAttendance");

            modelBuilder.Entity<AttendanceModel>()
                .HasOne(a => a.Employee)
                .WithMany()
                .HasForeignKey(a => a.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LeaveTypeModel>()
               .ToTable("tableleavetypes");

            modelBuilder.Entity<LeaveApplicationModel>()
               .ToTable("tableleaveapplications");

            modelBuilder.Entity<LeaveBalanceModel>()
               .ToTable("tableleavebalances");

        }
    }
}