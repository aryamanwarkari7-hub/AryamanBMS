using AryamanBMS.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUserModel>
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<DepartmentModel> Departments { get; set; }

        public DbSet<DesignationModel> Designations { get; set; }

        // Employee
        public DbSet<EmployeeModel> Employees { get; set; }
        public DbSet<StateModel> States { get; set; }
        public DbSet<CityModel> Cities { get; set; }
        public DbSet<PincodeModel> Pincodes { get; set; }
        public DbSet<EmployeeAcademicModel> EmployeeAcademics { get; set; }
        public DbSet<EmployeeDocumentModel> EmployeeDocuments { get; set; }
        public DbSet<EmployeePreviousEmploymentModel> EmployeePreviousEmployments
        { get; set; }

        public DbSet<AttendanceModel> Attendances { get; set; }


        // Leave
        public DbSet<LeaveTypeModel> LeaveTypes { get; set; }

        public DbSet<LeaveApplicationModel> LeaveApplications { get; set; }

        public DbSet<LeaveBalanceModel> LeaveBalances { get; set; }
        public DbSet<CompOffCreditModel> CompOffCredits { get; set; }

        // Salary
        public DbSet<SalaryRecordModel> SalaryRecords { get; set; }

        // Letters
        public DbSet<LetterModel> Letters { get; set; }

        // Projects
        public DbSet<ProjectModel> Projects { get; set; }
        public DbSet<ProjectMemberModel> ProjectMembers { get; set; }
        public DbSet<ProjectTaskModel> ProjectTasks { get; set; }
        public DbSet<ProjectFlowModel> ProjectFlows { get; set; }
        public DbSet<ProjectTaskProgressModel> ProjectTaskProgresses { get; set; }

        // Meetings
        public DbSet<ProjectMeetingModel> ProjectMeetings { get; set; }

        public DbSet<ProjectMeetingAttendeeModel> ProjectMeetingAttendees { get; set; }

        public DbSet<ProjectMeetingActionItemModel> ProjectMeetingActionItems { get; set; }

        // Risk
        public DbSet<ProjectRiskModel> ProjectRisks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Department
            modelBuilder.Entity<DepartmentModel>()
                .ToTable("TableDepartment");

            // Designation
            modelBuilder.Entity<DesignationModel>()
                .ToTable("TableDesignation");

            // Employee
            modelBuilder.Entity<EmployeeModel>()
                .ToTable("TableEmployee");

            modelBuilder.Entity<StateModel>()
            .ToTable("TableState");

            modelBuilder.Entity<StateModel>()
                .HasIndex(x => x.StateName)
                .IsUnique();

            modelBuilder.Entity<CityModel>()
                .ToTable("TableCity");

            modelBuilder.Entity<CityModel>()
                .HasIndex(x => new
                {
                    x.StateId,
                    x.CityName
                })
                .IsUnique();

            modelBuilder.Entity<CityModel>()
                .HasOne(x => x.State)
                .WithMany(x => x.Cities)
                .HasForeignKey(x => x.StateId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PincodeModel>()
                .ToTable("TablePincode");

            modelBuilder.Entity<PincodeModel>()
                .HasOne(x => x.City)
                .WithMany(x => x.Pincodes)
                .HasForeignKey(x => x.CityId)
                .OnDelete(DeleteBehavior.Restrict);

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

            modelBuilder.Entity<EmployeePreviousEmploymentModel>()
               .ToTable("TableEmployeePreviousEmployment");

            modelBuilder.Entity<EmployeePreviousEmploymentModel>()
                .HasOne(x => x.Employee)
                .WithMany(x => x.PreviousEmployments)
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<EmployeeDocumentModel>()
                .HasOne(x => x.PreviousEmployment)
                .WithMany(x => x.Documents)
                .HasForeignKey(x => x.EmployeePreviousEmploymentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Attendance : Employee relationship
            modelBuilder.Entity<AttendanceModel>()
                .ToTable("TableAttendance");

            modelBuilder.Entity<AttendanceModel>()
                .HasOne(a => a.Employee)
                .WithMany()
                .HasForeignKey(a => a.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Leave 
            modelBuilder.Entity<LeaveTypeModel>()
               .ToTable("tableleavetypes");

            modelBuilder.Entity<LeaveApplicationModel>()
               .ToTable("tableleaveapplications");

            modelBuilder.Entity<LeaveBalanceModel>()
                .ToTable("tableleavebalances");

            modelBuilder.Entity<CompOffCreditModel>()
                .ToTable("tablecompoffcredit");

            modelBuilder.Entity<CompOffCreditModel>()
                .HasOne(x => x.Employee)
                .WithMany()
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CompOffCreditModel>()
                .HasOne(x => x.Attendance)
                .WithMany()
                .HasForeignKey(x => x.AttendanceId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<CompOffCreditModel>()
               .Property(x => x.CreditDays)
               .HasPrecision(10, 2);

            // Salary Record
            modelBuilder.Entity<SalaryRecordModel>()
               .ToTable("TableSalaryRecord");

            // Letter
            modelBuilder.Entity<LetterModel>()
               .ToTable("TableLetters");

            // Project
            modelBuilder.Entity<ProjectModel>()
               .ToTable("TableProject");

            modelBuilder.Entity<ProjectModel>()
                .HasIndex(p => p.ProjectCode)
                .IsUnique();

            modelBuilder.Entity<ProjectModel>()
                .Property(p => p.Budget)
                .HasPrecision(18, 2);

            modelBuilder.Entity<ProjectModel>()
                .HasOne(p => p.ProjectManager)
                .WithMany()
                .HasForeignKey(p => p.ProjectManagerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Project Member
            modelBuilder.Entity<ProjectMemberModel>()
                .ToTable("TableProjectMember");

            modelBuilder.Entity<ProjectMemberModel>()
                .HasIndex(pm => new
                {
                    pm.ProjectId,
                    pm.EmployeeId
                })
                .IsUnique();

            modelBuilder.Entity<ProjectMemberModel>()
                .HasOne(pm => pm.Project)
                .WithMany()
                .HasForeignKey(pm => pm.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProjectMemberModel>()
                .HasOne(pm => pm.Employee)
                .WithMany()
                .HasForeignKey(pm => pm.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Project Task
            modelBuilder.Entity<ProjectTaskModel>()
                 .ToTable("TableProjectTask");

            modelBuilder.Entity<ProjectTaskModel>()
                .HasIndex(t => new
                {
                    t.ProjectId,
                    t.TaskCode
                })
                .IsUnique();

            modelBuilder.Entity<ProjectTaskModel>()
                .Property(t => t.EstimatedHours)
                .HasPrecision(10, 2);

            modelBuilder.Entity<ProjectTaskModel>()
                .Property(t => t.ActualHours)
                .HasPrecision(10, 2);

            modelBuilder.Entity<ProjectTaskModel>()
                .HasOne(t => t.Project)
                .WithMany()
                .HasForeignKey(t => t.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProjectTaskModel>()
                .HasOne(t => t.AssignedEmployee)
                .WithMany()
                .HasForeignKey(t => t.AssignedEmployeeId)
                .OnDelete(DeleteBehavior.SetNull);

            // Project Flow
            modelBuilder.Entity<ProjectFlowModel>()
                .ToTable("TableProjectFlow");

            modelBuilder.Entity<ProjectFlowModel>()
                .HasIndex(pf => new
                {
                    pf.ProjectId,
                    pf.StageOrder
                })
                .IsUnique();

            modelBuilder.Entity<ProjectFlowModel>()
                .HasOne(pf => pf.Project)
                .WithMany()
                .HasForeignKey(pf => pf.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            //Project Task Progress
            modelBuilder.Entity<ProjectTaskProgressModel>()
              .ToTable("TableProjectTaskProgress");

            modelBuilder.Entity<ProjectTaskProgressModel>()
                .Property(p => p.HoursWorked)
                .HasPrecision(5, 2);

            modelBuilder.Entity<ProjectTaskProgressModel>()
                .HasOne(p => p.ProjectTask)
                .WithMany()
                .HasForeignKey(p => p.ProjectTaskId)
                .OnDelete(DeleteBehavior.Cascade);

            // Meetings
            modelBuilder.Entity<ProjectMeetingModel>()
    .ToTable("TableProjectMeeting");

            modelBuilder.Entity<ProjectMeetingModel>()
                .HasOne(m => m.Project)
                .WithMany()
                .HasForeignKey(m => m.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);


            modelBuilder.Entity<ProjectMeetingAttendeeModel>()
                .ToTable("TableProjectMeetingAttendee");

            modelBuilder.Entity<ProjectMeetingAttendeeModel>()
                .HasIndex(a => new
                {
                    a.MeetingId,
                    a.EmployeeId
                })
                .IsUnique();

            modelBuilder.Entity<ProjectMeetingAttendeeModel>()
                .HasOne(a => a.Meeting)
                .WithMany(m => m.Attendees)
                .HasForeignKey(a => a.MeetingId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProjectMeetingAttendeeModel>()
                .HasOne(a => a.Employee)
                .WithMany()
                .HasForeignKey(a => a.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);


            modelBuilder.Entity<ProjectMeetingActionItemModel>()
                .ToTable("TableProjectMeetingActionItem");

            modelBuilder.Entity<ProjectMeetingActionItemModel>()
                .HasOne(a => a.Meeting)
                .WithMany(m => m.ActionItems)
                .HasForeignKey(a => a.MeetingId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProjectMeetingActionItemModel>()
                .HasOne(a => a.AssignedEmployee)
                .WithMany()
                .HasForeignKey(a => a.AssignedEmployeeId)
                .OnDelete(DeleteBehavior.SetNull);

            // Risk
            modelBuilder.Entity<ProjectRiskModel>()
                .ToTable("TableProjectRisk");

            modelBuilder.Entity<ProjectRiskModel>()
                .HasOne(r => r.Project)
                .WithMany()
                .HasForeignKey(r => r.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProjectRiskModel>()
                .HasOne(r => r.RiskOwnerEmployee)
                .WithMany()
                .HasForeignKey(r => r.RiskOwnerEmployeeId)
                .OnDelete(DeleteBehavior.SetNull);

        }
    }
}