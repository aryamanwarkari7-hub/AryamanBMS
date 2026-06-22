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

        public DbSet<EmployeeAcademicModel> EmployeeAcademics { get; set; }
        public DbSet<EmployeeDocumentModel> EmployeeDocuments { get; set; }

        public DbSet<AttendanceModel> Attendances { get; set; }


        // Leave
        public DbSet<LeaveTypeModel> LeaveTypes { get; set; }

        public DbSet<LeaveApplicationModel> LeaveApplications { get; set; }

        public DbSet<LeaveBalanceModel> LeaveBalances { get; set; }

        // Salary

        public DbSet<SalaryRecordModel> SalaryRecords { get; set; }

        // Letters
        public DbSet<LetterModel> Letters { get; set; } 

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<DepartmentModel>()
                .ToTable("TableDepartment");

            modelBuilder.Entity<DesignationModel>()
                .ToTable("TableDesignation");

            modelBuilder.Entity<EmployeeModel>()
                .ToTable("TableEmployee");

            modelBuilder.Entity<EmployeeAcademicModel>()
                 .ToTable("TableEmployeeAcademic");

            modelBuilder.Entity<EmployeeDocumentModel>()
                .ToTable("TableEmployeeDocument");

            modelBuilder.Entity<EmployeeAcademicModel>()
                .HasOne(x => x.Employee)
                .WithMany(x => x.AcademicRecords)
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<EmployeeDocumentModel>()
                .HasOne(x => x.Employee)
                .WithMany(x => x.Documents)
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<EmployeeDocumentModel>()
                .HasOne(x => x.EmployeeAcademic)
                .WithMany(x => x.Documents)
                .HasForeignKey(x => x.EmployeeAcademicId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<EmployeeAcademicModel>()
                .Property(x => x.Score)
                .HasPrecision(6, 2);

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

            modelBuilder.Entity<SalaryRecordModel>()
               .ToTable("TableSalaryRecord");

            modelBuilder.Entity<LetterModel>()
               .ToTable("TableLetters");

        }
    }
}