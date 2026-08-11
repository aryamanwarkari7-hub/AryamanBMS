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
        public DbSet<SalaryImportBatchModel> SalaryImportBatches { get; set; }
        public DbSet<PayrollPolicyModel> PayrollPolicies { get; set; }
        public DbSet<PayrollPeriodLockModel> PayrollPeriodLocks { get; set; }
        public DbSet<SalaryAdvanceModel> SalaryAdvances { get; set; }
        public DbSet<SalaryPaymentBatchModel> SalaryPaymentBatches { get; set; }
        public DbSet<FullAndFinalSettlementModel> FullAndFinalSettlements { get; set; }
        public DbSet<ProfessionalTaxSlabModel> ProfessionalTaxSlabs { get; set; }

        // Letters
        public DbSet<LetterModel> Letters { get; set; }

        // Projects
        public DbSet<ProjectModel> Projects { get; set; }
        public DbSet<ProjectMemberModel> ProjectMembers { get; set; }
        public DbSet<ProjectTaskModel> ProjectTasks { get; set; }
        public DbSet<ProjectFlowModel> ProjectFlows { get; set; }
        public DbSet<ProjectTaskProgressModel> ProjectTaskProgresses { get; set; }
        public DbSet<ProjectTimelineModel> ProjectTimelines { get; set; }
        public DbSet<ProjectCommunicationModel>ProjectCommunications{ get; set; }

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

        public DbSet<ClientCommunicationModel> ClientCommunications { get; set; }
        public DbSet<ProposalModel> Proposals { get; set; }
        public DbSet<ProposalTemplateModel>ProposalTemplates
        { get; set; }
        public DbSet<ProposalDocumentVersionModel>ProposalDocumentVersions { get; set; }

        public DbSet<ProposalAuditModel> ProposalAudits { get; set; }

        public DbSet<PurchaseOrderModel> PurchaseOrders { get; set; }

        public DbSet<BillingMilestoneModel> BillingMilestones { get; set; }

        public DbSet<InvoiceModel> Invoices { get; set; }
        public DbSet<InvoiceDetailsModel> InvoiceDetails { get; set; }

        public DbSet<InvoiceDocumentVersionModel> InvoiceDocumentVersions { get; set; }
        public DbSet<PaymentReceiptModel> PaymentReceipts { get; set; }

        public DbSet<AdvanceReceiptModel> AdvanceReceipts { get; set; }
        public DbSet<CreditNoteModel> CreditNotes { get; set; }
        public DbSet<DebitNoteModel> DebitNotes { get; set; }

        public DbSet<ExpenseCategoryModel> ExpenseCategories { get; set; }
        public DbSet<VendorModel> Vendors { get; set; }
        public DbSet<ExpenseVoucherModel> ExpenseVouchers { get; set; }
        public DbSet<ExpenseVoucherDocumentModel> ExpenseVoucherDocuments { get; set; }
        public DbSet<VendorPaymentModel> VendorPayments { get; set; }

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
        public DbSet<OfficeAssetDocumentModel> OfficeAssetDocuments { get; set; }
        public DbSet<OfficeAssetMaintenanceModel> OfficeAssetMaintenances { get; set; }
        public DbSet<OfficeAssetVerificationModel> OfficeAssetVerifications { get; set; }

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

        // Notification
        public DbSet<NotificationModel> TableNotification { get; set; }

        // Calendar
        public DbSet<CalendarManualEventModel> CalendarManualEvents { get; set; }

        //Holiday
        public DbSet<HolidayModel> Holidays { get; set; }

        // Override Saturday Working (optional)
        public DbSet<WorkingDayOverrideModel> WorkingDayOverrides { get; set; }

        //Login History
        public DbSet<LoginHistoryModel> TableLoginHistory { get; set; }
        public DbSet<PasswordChangeLogModel> PasswordChangeLogs { get; set; }

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

            modelBuilder.Entity<AttendanceModel>()
                .Property(a => a.AttendanceValue)
                .HasPrecision(4, 2);

            // Leave 
            modelBuilder.Entity<LeaveTypeModel>()
               .ToTable("tableleavetypes");

            modelBuilder.Entity<LeaveApplicationModel>()
               .ToTable("tableleaveapplications");

            modelBuilder.Entity<LeaveApplicationModel>()
                .Property(x => x.NumberOfDays)
                .HasPrecision(4, 2);

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

            modelBuilder.Entity<SalaryRecordModel>()
                .HasIndex(x => new
                {
                    x.EmployeeId,
                    x.Month,
                    x.Year
                })
                .IsUnique();

            modelBuilder.Entity<SalaryRecordModel>()
                .HasOne(x => x.Employee)
                .WithMany()
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SalaryRecordModel>()
                .HasOne(x => x.SalaryImportBatch)
                .WithMany(x => x.SalaryRecords)
                .HasForeignKey(x => x.SalaryImportBatchId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<SalaryImportBatchModel>()
                .ToTable("TableSalaryImportBatch");

            modelBuilder.Entity<PayrollPolicyModel>()
                .ToTable("TablePayrollPolicy");

            modelBuilder.Entity<PayrollPolicyModel>()
                .HasIndex(x => x.IsActive);

            modelBuilder.Entity<PayrollPeriodLockModel>()
                .ToTable("TablePayrollPeriodLock");

            modelBuilder.Entity<PayrollPeriodLockModel>()
                .HasIndex(x => new
                {
                    x.Month,
                    x.Year
                })
                .IsUnique();

            modelBuilder.Entity<SalaryAdvanceModel>()
                .ToTable("TableSalaryAdvance");

            modelBuilder.Entity<SalaryAdvanceModel>()
                .HasOne(x => x.Employee)
                .WithMany()
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SalaryPaymentBatchModel>()
                .ToTable("TableSalaryPaymentBatch");

            modelBuilder.Entity<SalaryPaymentBatchModel>()
                .HasIndex(x => new
                {
                    x.Month,
                    x.Year,
                    x.PaymentStatus
                });

            modelBuilder.Entity<FullAndFinalSettlementModel>()
                .ToTable("TableFullAndFinalSettlement");

            modelBuilder.Entity<FullAndFinalSettlementModel>()
                .HasOne(x => x.Employee)
                .WithMany()
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProfessionalTaxSlabModel>()
                .ToTable("TableProfessionalTaxSlab");

            modelBuilder.Entity<ProfessionalTaxSlabModel>()
                .HasIndex(x => new
                {
                    x.State,
                    x.IsActive
                });

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
                .WithMany(e => e.ManagedProjects)
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

            // Project Timeline
            modelBuilder.Entity<ProjectCommunicationModel>()
                .ToTable("tableprojectcommunications");

            modelBuilder.Entity<ProjectCommunicationModel>()
                .HasOne(x => x.Project)
                .WithMany()
                .HasForeignKey(x => x.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProjectCommunicationModel>()
                .HasOne(x => x.CreatedByEmployee)
                .WithMany()
                .HasForeignKey(x => x.CreatedByEmployeeId)
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

            // ACCOUNTS

            // Accounts & Finance
            modelBuilder.Entity<ClientModel>().ToTable("tableclientmaster");
            modelBuilder.Entity<ClientModel>()
                .HasIndex(x => x.ClientCode)
                .IsUnique();

            modelBuilder.Entity<ClientCommunicationModel>()
    .ToTable("tableclientcommunications");

            modelBuilder.Entity<ClientCommunicationModel>()
                .HasOne(x => x.Client)
                .WithMany()
                .HasForeignKey(x => x.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ClientCommunicationModel>()
                .HasOne(x => x.AssignedToEmployee)
                .WithMany()
                .HasForeignKey(x => x.AssignedToEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ClientCommunicationModel>()
                .HasOne(x => x.Proposal)
                .WithMany()
                .HasForeignKey(x => x.ProposalId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ClientCommunicationModel>()
                .HasOne(x => x.Project)
                .WithMany()
                .HasForeignKey(x => x.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ClientCommunicationModel>()
                .HasOne(x => x.Invoice)
                .WithMany()
                .HasForeignKey(x => x.InvoiceId)
                .OnDelete(DeleteBehavior.Restrict);

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

            modelBuilder.Entity<ProposalAuditModel>()
                .ToTable("tableproposalaudit");

            modelBuilder.Entity<ProposalAuditModel>()
                .HasOne(x => x.Proposal)
                .WithMany(x => x.AuditTrail)
                .HasForeignKey(x => x.ProposalId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProposalTemplateModel>()
                 .HasKey(x => x.ProposalTemplateId);

            modelBuilder.Entity<ProposalDocumentVersionModel>()
                .HasKey(x => x.ProposalDocumentVersionId);
            modelBuilder.Entity<ProposalTemplateModel>()
                .ToTable("TableProposalTemplate");

            modelBuilder.Entity<ProposalDocumentVersionModel>()
                .ToTable("TableProposalDocumentVersion");

            modelBuilder.Entity<ProposalTemplateModel>()
                .HasIndex(x => new
                {
                    x.TemplateName,
                    x.VersionNumber
                })
                .IsUnique();

            modelBuilder.Entity<ProposalDocumentVersionModel>()
                .HasOne(x => x.Proposal)
                .WithMany(x => x.DocumentVersions)
                .HasForeignKey(x => x.ProposalId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProposalDocumentVersionModel>()
                .HasOne(x => x.ProposalTemplate)
                .WithMany(x => x.ProposalDocuments)
                .HasForeignKey(x => x.ProposalTemplateId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProposalModel>()
                .HasOne(x => x.ProposalTemplate)
                .WithMany()
                .HasForeignKey(x => x.ProposalTemplateId)
                .OnDelete(DeleteBehavior.Restrict);

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

            modelBuilder.Entity<BillingMilestoneModel>()
                  .ToTable("tablebillingmilestone");

            modelBuilder.Entity<BillingMilestoneModel>()
                .HasOne(x => x.PurchaseWorkOrder)
                .WithMany()
                .HasForeignKey(x => x.PurchaseWorkOrderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BillingMilestoneModel>()
                .HasOne(x => x.Project)
                .WithMany()
                .HasForeignKey(x => x.ProjectId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<InvoiceModel>()
                .HasOne(x => x.BillingMilestone)
                .WithMany()
                .HasForeignKey(x => x.BillingMilestoneId)
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

            modelBuilder.Entity<InvoiceDocumentVersionModel>()
                 .ToTable("TableInvoiceDocumentVersion");

            modelBuilder.Entity<InvoiceDocumentVersionModel>()
                .HasOne(x => x.Invoice)
                .WithMany(x => x.DocumentVersions)
                .HasForeignKey(x => x.InvoiceId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<InvoiceDocumentVersionModel>()
                .HasIndex(x => new
                {
                    x.InvoiceId,
                    x.VersionNumber,
                    x.DocumentFormat
                })
                .IsUnique();

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

            // Advance receipt
            modelBuilder.Entity<AdvanceReceiptModel>()
                .ToTable("tableadvancereceipt");

            modelBuilder.Entity<AdvanceReceiptModel>()
                .HasIndex(x => x.AdvanceReceiptNo)
                .IsUnique();

            modelBuilder.Entity<AdvanceReceiptModel>()
                .HasOne(x => x.Client)
                .WithMany()
                .HasForeignKey(x => x.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AdvanceReceiptModel>()
                .HasOne(x => x.Project)
                .WithMany()
                .HasForeignKey(x => x.ProjectId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<CreditNoteModel>()
                   .ToTable("tablecreditnote");

            modelBuilder.Entity<CreditNoteModel>()
                .HasIndex(x => x.CreditNoteNo)
                .IsUnique();

            modelBuilder.Entity<CreditNoteModel>()
                .HasOne(x => x.OriginalInvoice)
                .WithMany()
                .HasForeignKey(x => x.OriginalInvoiceId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DebitNoteModel>()
                .ToTable("tabledebitnote");

            modelBuilder.Entity<DebitNoteModel>()
                .HasIndex(x => x.DebitNoteNo)
                .IsUnique();

            modelBuilder.Entity<DebitNoteModel>()
                .HasOne(x => x.OriginalInvoice)
                .WithMany()
                .HasForeignKey(x => x.OriginalInvoiceId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ExpenseCategoryModel>().ToTable("tableexpensecategories");
            modelBuilder.Entity<ExpenseCategoryModel>()
                .HasIndex(x => x.CategoryCode)
                .IsUnique();

            modelBuilder.Entity<VendorModel>().ToTable("tablevendor");
            modelBuilder.Entity<VendorModel>()
                .HasIndex(x => x.VendorCode)
                .IsUnique();
            modelBuilder.Entity<VendorModel>()
                .HasIndex(x => x.GSTIN);
            modelBuilder.Entity<VendorModel>()
                .HasIndex(x => x.IsActive);

            modelBuilder.Entity<ExpenseVoucherModel>().ToTable("tableexpensevouchers");
            modelBuilder.Entity<ExpenseVoucherModel>()
                .HasIndex(x => x.VoucherNumber)
                .IsUnique();
            modelBuilder.Entity<ExpenseVoucherModel>()
                .HasIndex(x => new
                {
                    x.VendorId,
                    x.InvoiceNumber,
                    x.FinancialYear
                });
            modelBuilder.Entity<ExpenseVoucherModel>()
                .HasOne(x => x.Category)
                .WithMany(x => x.ExpenseVouchers)
                .HasForeignKey(x => x.ExpenseCategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ExpenseVoucherModel>()
                .HasOne(x => x.Vendor)
                .WithMany(x => x.ExpenseVouchers)
                .HasForeignKey(x => x.VendorId)
                .OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<ExpenseVoucherModel>()
                .HasOne(x => x.Project)
                .WithMany()
                .HasForeignKey(x => x.ProjectId)
                .OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<ExpenseVoucherModel>()
                .HasOne(x => x.Department)
                .WithMany()
                .HasForeignKey(x => x.DepartmentId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ExpenseVoucherDocumentModel>().ToTable("tableexpensevoucherdocument");
            modelBuilder.Entity<ExpenseVoucherDocumentModel>()
                .HasOne(x => x.ExpenseVoucher)
                .WithMany(x => x.Documents)
                .HasForeignKey(x => x.ExpenseVoucherId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<VendorPaymentModel>().ToTable("tablevendorpayment");
            modelBuilder.Entity<VendorPaymentModel>()
                .HasIndex(x => x.PaymentNo)
                .IsUnique();
            modelBuilder.Entity<VendorPaymentModel>()
                .HasIndex(x => x.TransactionReference);
            modelBuilder.Entity<VendorPaymentModel>()
                .HasOne(x => x.Vendor)
                .WithMany()
                .HasForeignKey(x => x.VendorId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<VendorPaymentModel>()
                .HasOne(x => x.ExpenseVoucher)
                .WithMany(x => x.VendorPayments)
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
            modelBuilder.Entity<OfficeAssetModel>()
                .HasOne(x => x.Vendor)
                .WithMany()
                .HasForeignKey(x => x.VendorId)
                .OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<OfficeAssetModel>()
                .HasOne(x => x.ExpenseVoucher)
                .WithMany()
                .HasForeignKey(x => x.ExpenseVoucherId)
                .OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<OfficeAssetModel>()
                .HasOne(x => x.PurchaseOrder)
                .WithMany()
                .HasForeignKey(x => x.PurchaseOrderId)
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

            modelBuilder.Entity<OfficeAssetDocumentModel>()
                .ToTable("tableofficeassetdocument");
            modelBuilder.Entity<OfficeAssetDocumentModel>()
                .HasOne(x => x.OfficeAsset)
                .WithMany(x => x.Documents)
                .HasForeignKey(x => x.OfficeAssetId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OfficeAssetMaintenanceModel>()
                .ToTable("tableofficeassetmaintenance");
            modelBuilder.Entity<OfficeAssetMaintenanceModel>()
                .HasOne(x => x.OfficeAsset)
                .WithMany(x => x.MaintenanceHistory)
                .HasForeignKey(x => x.OfficeAssetId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OfficeAssetVerificationModel>()
                .ToTable("tableofficeassetverification");
            modelBuilder.Entity<OfficeAssetVerificationModel>()
                .HasOne(x => x.OfficeAsset)
                .WithMany(x => x.VerificationHistory)
                .HasForeignKey(x => x.OfficeAssetId)
                .OnDelete(DeleteBehavior.Cascade);

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

            // Notification
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

            //Calendar Events
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

            // Holiday
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

            // Saturday working override
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

            // Login History
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

            modelBuilder.Entity<PasswordChangeLogModel>()
               .ToTable("tablepasswordchangelogs");

        }
    }
}
