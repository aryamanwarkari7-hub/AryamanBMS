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
        public DbSet<CompOffUsageModel> CompOffUsages { get; set; }

        // Salary
        public DbSet<SalaryRecordModel> SalaryRecords { get; set; }
        public DbSet<EmployeeSalaryStructureModel> EmployeeSalaryStructures { get; set; }

        // Letters
        public DbSet<LetterModel> Letters { get; set; }

        // Projects
        public DbSet<ProjectModel> Projects { get; set; }
        public DbSet<ProjectMemberModel> ProjectMembers { get; set; }
        public DbSet<ProjectTaskModel> ProjectTasks { get; set; }
        public DbSet<ProjectFlowModel> ProjectFlows { get; set; }
        public DbSet<ProjectTaskProgressModel> ProjectTaskProgresses { get; set; }
        public DbSet<ProjectTimelineModel> ProjectTimelines { get; set; }

        // Meetings
        public DbSet<ProjectMeetingModel> ProjectMeetings { get; set; }

        public DbSet<ProjectMeetingAttendeeModel> ProjectMeetingAttendees { get; set; }

        public DbSet<ProjectMeetingActionItemModel> ProjectMeetingActionItems { get; set; }

        // Risk
        public DbSet<ProjectRiskModel> ProjectRisks { get; set; }

        // ACCOUNTS

        public DbSet<CompanyProfileModel> CompanyProfiles { get; set; }
        public DbSet<CompanyDocumentCategoryModel> CompanyDocumentCategories { get; set; }
        public DbSet<CompanyDocumentModel> CompanyDocuments { get; set; }

        public DbSet<ClientModel> Clients { get; set; }
        public DbSet<ProposalModel> Proposals { get; set; }
        public DbSet<PurchaseOrderModel> PurchaseOrders { get; set; }

        public DbSet<InvoiceModel> Invoices { get; set; }
        public DbSet<InvoiceDetailsModel> InvoiceDetails { get; set; }
        public DbSet<PaymentReceiptModel> PaymentReceipts { get; set; }

        public DbSet<ExpenseCategoryModel> ExpenseCategories { get; set; }
        public DbSet<ExpenseVoucherModel> ExpenseVouchers { get; set; }
        public DbSet<ExpenseVoucherDocumentModel> ExpenseVoucherDocuments { get; set; }

        public DbSet<GstMonthlySnapshotModel> GstMonthlySnapshots { get; set; }
        public DbSet<GstConfigurationModel> GstConfigurations { get; set; }
        public DbSet<GstReturnModel> GstReturns { get; set; }
        public DbSet<GstChallanModel> GstChallans { get; set; }
        public DbSet<GstItcRecordModel> GstItcRecords { get; set; }
        public DbSet<GstDocumentModel> GstDocuments { get; set; }

        public DbSet<FinancialAuditDocumentModel> FinancialAuditDocuments { get; set; }
        
        public DbSet<OfficeAssetModel> OfficeAssets { get; set; }
        public DbSet<OfficeAssetAssignmentHistoryModel> OfficeAssetAssignmentHistories
        { get; set; }

        public DbSet<PfMonthlySnapshotModel> PfMonthlySnapshots { get; set; }
        public DbSet<PfChallanModel> PfChallans { get; set; }
        public DbSet<PfDocumentModel> PfDocuments { get; set; }

        public DbSet<EsicMonthlySnapshotModel> EsicMonthlySnapshots { get; set; }
        public DbSet<EsicChallanModel> EsicChallans { get; set; }
        public DbSet<EsicDocumentModel> EsicDocuments { get; set; }

        public DbSet<PtMonthlySnapshotModel> PtMonthlySnapshots { get; set; }
        public DbSet<PtChallanModel> PtChallans { get; set; }
        public DbSet<PtDocumentModel> PtDocuments { get; set; }

        public DbSet<NoticeModel> Notices { get; set; }
        public DbSet<NoticeDocumentModel> NoticeDocuments { get; set; }

        public DbSet<FinancialSequenceModel> FinancialSequences { get; set; }

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

            modelBuilder.Entity<CompOffCreditModel>()
               .HasOne(x => x.LeaveApplication)
               .WithMany()
               .HasForeignKey(x => x.LeaveApplicationId)
               .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<CompOffCreditModel>()
                .Property(x => x.UsedDays)
                .HasPrecision(10, 2);

            modelBuilder.Entity<CompOffUsageModel>()
                 .ToTable("tablecompoffusage");

            modelBuilder.Entity<CompOffUsageModel>()
                .Property(x => x.UsedDays)
                .HasPrecision(10, 2);

            modelBuilder.Entity<CompOffUsageModel>()
                .HasOne(x => x.CompOffCredit)
                .WithMany()
                .HasForeignKey(x => x.CompOffCreditId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CompOffUsageModel>()
                .HasOne(x => x.LeaveApplication)
                .WithMany()
                .HasForeignKey(x => x.LeaveApplicationId)
                .OnDelete(DeleteBehavior.Restrict);

            // Salary Record
            modelBuilder.Entity<SalaryRecordModel>()
               .ToTable("TableSalaryRecord");
            modelBuilder.Entity<EmployeeSalaryStructureModel>()
               .ToTable("TableEmployeeSalaryStructure");

            modelBuilder.Entity<EmployeeSalaryStructureModel>()
                .Property(x => x.ActualSalary)
                .HasPrecision(18, 2);

            modelBuilder.Entity<EmployeeSalaryStructureModel>()
                .HasIndex(x => new
                {
                    x.EmployeeId,
                    x.EffectiveFrom,
                    x.EffectiveTo,
                    x.IsActive
                });

            modelBuilder.Entity<EmployeeSalaryStructureModel>()
                .HasOne(x => x.Employee)
                .WithMany()
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

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

            // Project Timeline
            modelBuilder.Entity<ProjectTimelineModel>(entity =>
            {
                entity.ToTable("TableProjectTimeline");

                entity.HasKey(x => x.Id);

                entity.HasOne(x => x.Project)
                    .WithMany()
                    .HasForeignKey(x => x.ProjectId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

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

            // ACCOUNTS

            // Accounts & Finance
            modelBuilder.Entity<ClientModel>().ToTable("tableclientmaster");
            modelBuilder.Entity<ClientModel>()
                .HasIndex(x => x.ClientCode)
                .IsUnique();

            modelBuilder.Entity<CompanyProfileModel>().ToTable("tablecompanyprofile");
            modelBuilder.Entity<CompanyProfileModel>()
                .HasIndex(x => x.GSTIN)
                .IsUnique();

            modelBuilder.Entity<CompanyDocumentCategoryModel>().ToTable("tablecompanydocumentcategory");
            modelBuilder.Entity<CompanyDocumentCategoryModel>()
                .HasIndex(x => x.CategoryName)
                .IsUnique();

            modelBuilder.Entity<CompanyDocumentModel>().ToTable("tablecompanydocument");
            modelBuilder.Entity<CompanyDocumentModel>()
                .HasIndex(x => x.IsActive);
            modelBuilder.Entity<CompanyDocumentModel>()
                .HasOne(x => x.Category)
                .WithMany()
                .HasForeignKey(x => x.DocumentCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FinancialSequenceModel>().ToTable("tablefinancialsequence");
            modelBuilder.Entity<FinancialSequenceModel>()
                .HasIndex(x => new { x.DocumentType, x.FinancialYear })
                .IsUnique();

            modelBuilder.Entity<ProposalModel>().ToTable("tableproposal");
            modelBuilder.Entity<ProposalModel>()
                .HasIndex(x => x.ProposalNumber)
                .IsUnique();
            modelBuilder.Entity<ProposalModel>()
                .HasOne(x => x.Client)
                .WithMany(x => x.Proposals)
                .HasForeignKey(x => x.ClientId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ProposalModel>()
                .HasOne(x => x.Project)
                .WithMany()
                .HasForeignKey(x => x.ProjectId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<PurchaseOrderModel>().ToTable("tablepurchaseorder");
            modelBuilder.Entity<PurchaseOrderModel>()
                .HasIndex(x => x.OrderNumber)
                .IsUnique();
            modelBuilder.Entity<PurchaseOrderModel>()
                .HasOne(x => x.Client)
                .WithMany(x => x.PurchaseOrders)
                .HasForeignKey(x => x.ClientId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PurchaseOrderModel>()
                .HasOne(x => x.Proposal)
                .WithMany(x => x.PurchaseOrders)
                .HasForeignKey(x => x.ProposalId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<InvoiceModel>().ToTable("tableinvoicemaster");
            modelBuilder.Entity<InvoiceModel>()
                .HasIndex(x => x.InvoiceNo)
                .IsUnique();
            modelBuilder.Entity<InvoiceModel>()
                .HasOne(x => x.Client)
                .WithMany()
                .HasForeignKey(x => x.ClientId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<InvoiceModel>()
                .HasMany(x => x.InvoiceDetails)
                .WithOne(x => x.Invoice)
                .HasForeignKey(x => x.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
            

            modelBuilder.Entity<InvoiceDetailsModel>().ToTable("tableinvoicedetails");

            modelBuilder.Entity<PaymentReceiptModel>().ToTable("tablepaymentreceipt");
            modelBuilder.Entity<PaymentReceiptModel>()
                .HasIndex(x => x.ReceiptNo)
                .IsUnique();
            modelBuilder.Entity<PaymentReceiptModel>()
                .HasOne(x => x.Client)
                .WithMany()
                .HasForeignKey(x => x.ClientId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PaymentReceiptModel>()
                .HasOne(x => x.Invoice)
                .WithMany()
                .HasForeignKey(x => x.InvoiceId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ExpenseCategoryModel>().ToTable("tableexpensecategories");
            modelBuilder.Entity<ExpenseCategoryModel>()
                .HasIndex(x => x.CategoryCode)
                .IsUnique();

            modelBuilder.Entity<ExpenseVoucherModel>().ToTable("tableexpensevouchers");
            modelBuilder.Entity<ExpenseVoucherModel>()
                .HasIndex(x => x.VoucherNumber)
                .IsUnique();
            modelBuilder.Entity<ExpenseVoucherModel>()
                .HasOne(x => x.Category)
                .WithMany(x => x.ExpenseVouchers)
                .HasForeignKey(x => x.ExpenseCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ExpenseVoucherDocumentModel>().ToTable("tableexpensevoucherdocument");
            modelBuilder.Entity<ExpenseVoucherDocumentModel>()
                .HasOne(x => x.ExpenseVoucher)
                .WithMany(x => x.Documents)
                .HasForeignKey(x => x.ExpenseVoucherId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<GstMonthlySnapshotModel>().ToTable("tablegstmonthlysnapshot");
            modelBuilder.Entity<GstMonthlySnapshotModel>()
                .HasIndex(x => new { x.Month, x.Year })
                .IsUnique();

            modelBuilder.Entity<GstConfigurationModel>().ToTable("tablegstconfiguration");
            modelBuilder.Entity<GstConfigurationModel>()
                .HasIndex(x => x.IsActive);

            modelBuilder.Entity<GstReturnModel>().ToTable("tablegstreturn");
            modelBuilder.Entity<GstReturnModel>()
                .HasOne(x => x.Snapshot)
                .WithMany(x => x.Returns)
                .HasForeignKey(x => x.SnapshotId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<GstChallanModel>().ToTable("tablegstchallan");
            modelBuilder.Entity<GstChallanModel>()
                .HasOne(x => x.Snapshot)
                .WithMany(x => x.Challans)
                .HasForeignKey(x => x.SnapshotId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<GstItcRecordModel>().ToTable("tablegstitcrecord");
            modelBuilder.Entity<GstItcRecordModel>()
                .HasOne(x => x.Snapshot)
                .WithMany(x => x.ItcRecords)
                .HasForeignKey(x => x.SnapshotId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<GstDocumentModel>().ToTable("tablegstdocument");
            modelBuilder.Entity<GstDocumentModel>()
                .HasOne(x => x.Snapshot)
                .WithMany(x => x.Documents)
                .HasForeignKey(x => x.SnapshotId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FinancialAuditDocumentModel>().ToTable("tablefinancialauditdocuments");
            modelBuilder.Entity<FinancialAuditDocumentModel>()
                .HasIndex(x => x.FinancialYear);
            modelBuilder.Entity<FinancialAuditDocumentModel>()
                .HasIndex(x => x.DocumentCategory);
            modelBuilder.Entity<FinancialAuditDocumentModel>()
                .HasIndex(x => x.IsActive);
            modelBuilder.Entity<FinancialAuditDocumentModel>()
                .HasIndex(x => x.IsFinalized);

            modelBuilder.Entity<OfficeAssetModel>().ToTable("tableofficeasset");
            modelBuilder.Entity<OfficeAssetModel>()
                .HasIndex(x => x.AssetCode)
                .IsUnique();
            modelBuilder.Entity<OfficeAssetModel>()
                .HasIndex(x => x.IsActive);
            modelBuilder.Entity<OfficeAssetModel>()
                .HasOne(x => x.AssignedEmployee)
                .WithMany()
                .HasForeignKey(x => x.AssignedEmployeeId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<OfficeAssetAssignmentHistoryModel>()
                .ToTable("tableofficeassetassignmenthistory");
            modelBuilder.Entity<OfficeAssetAssignmentHistoryModel>()
                .HasIndex(x => new { x.OfficeAssetId, x.IsActive });
            modelBuilder.Entity<OfficeAssetAssignmentHistoryModel>()
                .HasIndex(x => x.EmployeeId);
            modelBuilder.Entity<OfficeAssetAssignmentHistoryModel>()
                .HasOne(x => x.OfficeAsset)
                .WithMany(x => x.AssignmentHistory)
                .HasForeignKey(x => x.OfficeAssetId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<OfficeAssetAssignmentHistoryModel>()
                .HasOne(x => x.Employee)
                .WithMany()
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PfMonthlySnapshotModel>().ToTable("tablepfmonthlysnapshot");
            modelBuilder.Entity<PfMonthlySnapshotModel>()
                .HasIndex(x => new { x.Month, x.Year })
                .IsUnique();

            modelBuilder.Entity<PfChallanModel>().ToTable("tablepfchallan");
            modelBuilder.Entity<PfChallanModel>()
                .HasOne(x => x.Snapshot)
                .WithMany(x => x.Challans)
                .HasForeignKey(x => x.PfSnapshotId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PfDocumentModel>().ToTable("tablepfdocument");
            modelBuilder.Entity<PfDocumentModel>()
                .HasIndex(x => x.IsActive);
            modelBuilder.Entity<PfDocumentModel>()
                .HasOne(x => x.Snapshot)
                .WithMany(x => x.Documents)
                .HasForeignKey(x => x.PfSnapshotId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<EsicMonthlySnapshotModel>().ToTable("tableesicmonthlysnapshot");
            modelBuilder.Entity<EsicMonthlySnapshotModel>()
                .HasIndex(x => new { x.Month, x.Year })
                .IsUnique();

            modelBuilder.Entity<EsicChallanModel>().ToTable("tableesicchallan");
            modelBuilder.Entity<EsicChallanModel>()
                .HasOne(x => x.Snapshot)
                .WithMany(x => x.Challans)
                .HasForeignKey(x => x.EsicSnapshotId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<EsicDocumentModel>().ToTable("tableesicdocument");
            modelBuilder.Entity<EsicDocumentModel>()
                .HasIndex(x => x.IsActive);
            modelBuilder.Entity<EsicDocumentModel>()
                .HasOne(x => x.Snapshot)
                .WithMany(x => x.Documents)
                .HasForeignKey(x => x.EsicSnapshotId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PtMonthlySnapshotModel>().ToTable("tableptmonthlysnapshot");
            modelBuilder.Entity<PtMonthlySnapshotModel>()
                .HasIndex(x => new { x.Month, x.Year })
                .IsUnique();

            modelBuilder.Entity<PtChallanModel>().ToTable("tableptchallan");
            modelBuilder.Entity<PtChallanModel>()
                .HasOne(x => x.Snapshot)
                .WithMany(x => x.Challans)
                .HasForeignKey(x => x.PtSnapshotId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PtDocumentModel>().ToTable("tableptdocument");
            modelBuilder.Entity<PtDocumentModel>()
                .HasIndex(x => x.IsActive);
            modelBuilder.Entity<PtDocumentModel>()
                .HasOne(x => x.Snapshot)
                .WithMany(x => x.Documents)
                .HasForeignKey(x => x.PtSnapshotId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<NoticeModel>().ToTable("tablenotice");
            modelBuilder.Entity<NoticeModel>()
                .HasIndex(x => x.IsActive);

            modelBuilder.Entity<NoticeDocumentModel>().ToTable("tablenoticedocument");
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
