-- Manual Salary / Payroll changes
-- Run manually in MySQL. No EF migration required.


ALTER TABLE tableemployeesalarystructure
ADD BasicSalary DECIMAL(18,2) NOT NULL DEFAULT 0,
ADD HRA DECIMAL(18,2) NOT NULL DEFAULT 0,
ADD DA DECIMAL(18,2) NOT NULL DEFAULT 0,
ADD Conveyance DECIMAL(18,2) NOT NULL DEFAULT 0,
ADD MedicalAllowance DECIMAL(18,2) NOT NULL DEFAULT 0,
ADD EducationAllowance DECIMAL(18,2) NOT NULL DEFAULT 0,
ADD SpecialAllowance DECIMAL(18,2) NOT NULL DEFAULT 0,
ADD OtherAllowances DECIMAL(18,2) NOT NULL DEFAULT 0,
ADD IsPfApplicable BIT NOT NULL DEFAULT 0,
ADD IsEsicApplicable BIT NOT NULL DEFAULT 0,
ADD IsPtApplicable BIT NOT NULL DEFAULT 0,
ADD IsTdsApplicable BIT NOT NULL DEFAULT 0,
ADD PreviousSalary DECIMAL(18,2) NOT NULL DEFAULT 0,
ADD RevisedSalary DECIMAL(18,2) NOT NULL DEFAULT 0,
ADD RevisionPercentage DECIMAL(8,2) NOT NULL DEFAULT 0,
ADD RevisionEffectiveDate DATETIME NULL,
ADD RevisionReason VARCHAR(500) NULL,
ADD ApprovedByUserId VARCHAR(450) NULL,
ADD ApprovedOn DATETIME NULL,
ADD CreatedByUserId VARCHAR(450) NULL,
ADD UpdatedByUserId VARCHAR(450) NULL;

ALTER TABLE tablesalaryrecord
ADD StandardMonthlySalary DECIMAL(18,2) NOT NULL DEFAULT 0,
ADD PayrollDivisor DECIMAL(10,2) NOT NULL DEFAULT 0,
ADD EligibleEmploymentDays DECIMAL(10,2) NOT NULL DEFAULT 0,
ADD PresentDaysValue DECIMAL(10,2) NOT NULL DEFAULT 0,
ADD PaidLeaveDays DECIMAL(10,2) NOT NULL DEFAULT 0,
ADD UnpaidLeaveDays DECIMAL(10,2) NOT NULL DEFAULT 0,
ADD LeaveWithoutPayDays DECIMAL(10,2) NOT NULL DEFAULT 0,
ADD WeeklyOffs DECIMAL(10,2) NOT NULL DEFAULT 0,
ADD Holidays DECIMAL(10,2) NOT NULL DEFAULT 0,
ADD OnDutyDays DECIMAL(10,2) NOT NULL DEFAULT 0,
ADD PerDaySalary DECIMAL(18,2) NOT NULL DEFAULT 0,
ADD ProratedSalary DECIMAL(18,2) NOT NULL DEFAULT 0,
ADD IsPfApplicable BIT NOT NULL DEFAULT 0,
ADD PfWage DECIMAL(18,2) NOT NULL DEFAULT 0,
ADD EmployeePfRate DECIMAL(8,2) NOT NULL DEFAULT 0,
ADD EmployerPfRate DECIMAL(8,2) NOT NULL DEFAULT 0,
ADD PensionComponent DECIMAL(18,2) NOT NULL DEFAULT 0,
ADD PfNonApplicabilityReason VARCHAR(500) NULL,
ADD IsEsicApplicable BIT NOT NULL DEFAULT 0,
ADD EsicWage DECIMAL(18,2) NOT NULL DEFAULT 0,
ADD EmployeeEsicRate DECIMAL(8,2) NOT NULL DEFAULT 0,
ADD EmployerEsicRate DECIMAL(8,2) NOT NULL DEFAULT 0,
ADD EsicNonApplicabilityReason VARCHAR(500) NULL,
ADD ProfessionalTaxState VARCHAR(100) NULL,
ADD ProfessionalTaxSlab VARCHAR(100) NULL,
ADD ProfessionalTaxExemptionReason VARCHAR(500) NULL,
ADD TaxRegime VARCHAR(30) NULL,
ADD EstimatedAnnualIncome DECIMAL(18,2) NOT NULL DEFAULT 0,
ADD PreviousEmployerIncome DECIMAL(18,2) NOT NULL DEFAULT 0,
ADD TaxExemptions DECIMAL(18,2) NOT NULL DEFAULT 0,
ADD ChapterSixDeductions DECIMAL(18,2) NOT NULL DEFAULT 0,
ADD OtherIncomeDeclared DECIMAL(18,2) NOT NULL DEFAULT 0,
ADD TaxAlreadyDeducted DECIMAL(18,2) NOT NULL DEFAULT 0,
ADD AnnualTaxLiability DECIMAL(18,2) NOT NULL DEFAULT 0,
ADD DeclarationStatus VARCHAR(30) NULL,
ADD Form12BBReference VARCHAR(150) NULL,
ADD Form16Reference VARCHAR(150) NULL,
ADD Bonus DECIMAL(18,2) NOT NULL DEFAULT 0,
ADD Incentive DECIMAL(18,2) NOT NULL DEFAULT 0,
ADD OvertimeHours DECIMAL(18,2) NOT NULL DEFAULT 0,
ADD OvertimeRate DECIMAL(18,2) NOT NULL DEFAULT 0,
ADD OvertimeAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
ADD Arrears DECIMAL(18,2) NOT NULL DEFAULT 0,
ADD Reimbursement DECIMAL(18,2) NOT NULL DEFAULT 0,
ADD LeaveEncashment DECIMAL(18,2) NOT NULL DEFAULT 0,
ADD OneTimeAdjustment DECIMAL(18,2) NOT NULL DEFAULT 0,
ADD BonusProvision DECIMAL(18,2) NOT NULL DEFAULT 0,
ADD GratuityProvision DECIMAL(18,2) NOT NULL DEFAULT 0,
ADD InsuranceBenefit DECIMAL(18,2) NOT NULL DEFAULT 0,
ADD OtherEmployerBenefits DECIMAL(18,2) NOT NULL DEFAULT 0,
ADD MonthlyCTC DECIMAL(18,2) NOT NULL DEFAULT 0,
ADD PayrollStatus VARCHAR(30) NOT NULL DEFAULT 'Draft',
ADD PaidByUserId VARCHAR(450) NULL,
ADD PaymentFailureReason VARCHAR(500) NULL,
ADD PaymentReversalReason VARCHAR(500) NULL,
ADD RetriedOn DATETIME NULL,
ADD RetriedByUserId VARCHAR(450) NULL,
ADD GeneratedByUserId VARCHAR(450) NULL,
ADD GeneratedOn DATETIME NULL,
ADD VerifiedByUserId VARCHAR(450) NULL,
ADD VerifiedOn DATETIME NULL,
ADD FinalizedByUserId VARCHAR(450) NULL,
ADD FinalizedOn DATETIME NULL,
ADD ReopenedByUserId VARCHAR(450) NULL,
ADD ReopenedOn DATETIME NULL,
ADD ReopenReason VARCHAR(500) NULL,
ADD IsPayslipReleased BIT NOT NULL DEFAULT 0,
ADD PayslipReleasedByUserId VARCHAR(450) NULL,
ADD PayslipReleasedOn DATETIME NULL,
ADD EmployeeViewedPayslipOn DATETIME NULL,
ADD SalaryImportBatchId INT NULL,
ADD ImportedByUserId VARCHAR(450) NULL,
ADD CreatedByUserId VARCHAR(450) NULL,
ADD CreatedOn DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
ADD UpdatedByUserId VARCHAR(450) NULL,
ADD UpdatedOn DATETIME NULL,
ADD JournalEntryReference VARCHAR(150) NULL,
ADD AccountingPostingDate DATETIME NULL;

CREATE UNIQUE INDEX UX_tablesalaryrecord_Employee_Month_Year
ON tablesalaryrecord (EmployeeId, Month, Year);

CREATE TABLE tablesalaryimportbatch (
    SalaryImportBatchId INT NOT NULL AUTO_INCREMENT,
    SourceFileName VARCHAR(250) NOT NULL,
    ImportedByUserId VARCHAR(450) NULL,
    ImportedOn DATETIME NOT NULL,
    Month INT NOT NULL,
    Year INT NOT NULL,
    TotalRows INT NOT NULL,
    SuccessfulRows INT NOT NULL,
    FailedRows INT NOT NULL,
    ErrorSummary LONGTEXT NULL,
    PRIMARY KEY (SalaryImportBatchId)
);

ALTER TABLE tablesalaryrecord
ADD CONSTRAINT FK_tablesalaryrecord_tablesalaryimportbatch
FOREIGN KEY (SalaryImportBatchId)
REFERENCES tablesalaryimportbatch (SalaryImportBatchId)
ON DELETE SET NULL;

CREATE TABLE tablepayrollpolicy (
    PayrollPolicyId INT NOT NULL AUTO_INCREMENT,
    PolicyName VARCHAR(100) NOT NULL,
    DivisorType VARCHAR(30) NOT NULL,
    RequireAttendanceClosure BIT NOT NULL,
    RequireLeaveClosure BIT NOT NULL,
    ReleasePayslipAfterPaymentOnly BIT NOT NULL,
    IsActive BIT NOT NULL,
    CreatedOn DATETIME NOT NULL,
    UpdatedOn DATETIME NULL,
    PRIMARY KEY (PayrollPolicyId)
);

CREATE TABLE tablepayrollperiodlock (
    PayrollPeriodLockId INT NOT NULL AUTO_INCREMENT,
    Month INT NOT NULL,
    Year INT NOT NULL,
    IsLocked BIT NOT NULL,
    LockedByUserId VARCHAR(450) NULL,
    LockedOn DATETIME NULL,
    ReopenedByUserId VARCHAR(450) NULL,
    ReopenedOn DATETIME NULL,
    ReopenReason VARCHAR(500) NULL,
    PRIMARY KEY (PayrollPeriodLockId),
    UNIQUE KEY UX_tablepayrollperiodlock_Month_Year (Month, Year)
);

CREATE TABLE tablesalaryadvance (
    SalaryAdvanceId INT NOT NULL AUTO_INCREMENT,
    EmployeeId INT NOT NULL,
    AdvanceAmount DECIMAL(18,2) NOT NULL,
    AdvanceDate DATETIME NOT NULL,
    ApprovedByUserId VARCHAR(450) NULL,
    ApprovedOn DATETIME NULL,
    RecoveryStartMonth INT NOT NULL,
    RecoveryStartYear INT NOT NULL,
    MonthlyRecoveryAmount DECIMAL(18,2) NOT NULL,
    TotalRecovered DECIMAL(18,2) NOT NULL,
    OutstandingBalance DECIMAL(18,2) NOT NULL,
    Status VARCHAR(30) NOT NULL,
    Remarks VARCHAR(500) NULL,
    CreatedOn DATETIME NOT NULL,
    UpdatedOn DATETIME NULL,
    PRIMARY KEY (SalaryAdvanceId),
    INDEX IX_tablesalaryadvance_EmployeeId (EmployeeId)
);

CREATE TABLE tablesalarypaymentbatch (
    SalaryPaymentBatchId INT NOT NULL AUTO_INCREMENT,
    Month INT NOT NULL,
    Year INT NOT NULL,
    BankAccount VARCHAR(100) NULL,
    PaymentDate DATETIME NULL,
    TotalEmployees INT NOT NULL,
    TotalNetSalary DECIMAL(18,2) NOT NULL,
    TransactionReference VARCHAR(150) NULL,
    UploadedBankFilePath VARCHAR(500) NULL,
    ProcessedByUserId VARCHAR(450) NULL,
    PaymentStatus VARCHAR(30) NOT NULL,
    FailureReason VARCHAR(500) NULL,
    ReversalReason VARCHAR(500) NULL,
    CreatedOn DATETIME NOT NULL,
    UpdatedOn DATETIME NULL,
    PRIMARY KEY (SalaryPaymentBatchId)
);

CREATE TABLE tablefullandfinalsettlement (
    FullAndFinalSettlementId INT NOT NULL AUTO_INCREMENT,
    EmployeeId INT NOT NULL,
    LastWorkingDate DATETIME NOT NULL,
    SalaryUpToLastWorkingDate DECIMAL(18,2) NOT NULL,
    LeaveEncashment DECIMAL(18,2) NOT NULL,
    BonusOrIncentive DECIMAL(18,2) NOT NULL,
    NoticePay DECIMAL(18,2) NOT NULL,
    SalaryAdvanceRecovery DECIMAL(18,2) NOT NULL,
    LoanRecovery DECIMAL(18,2) NOT NULL,
    AssetRecovery DECIMAL(18,2) NOT NULL,
    OtherPayableAmount DECIMAL(18,2) NOT NULL,
    OtherRecoverableAmount DECIMAL(18,2) NOT NULL,
    FinalNetPayable DECIMAL(18,2) NOT NULL,
    ApprovalStatus VARCHAR(30) NOT NULL,
    PaymentStatus VARCHAR(30) NOT NULL,
    ApprovedByUserId VARCHAR(450) NULL,
    ApprovedOn DATETIME NULL,
    CreatedOn DATETIME NOT NULL,
    UpdatedOn DATETIME NULL,
    PRIMARY KEY (FullAndFinalSettlementId),
    INDEX IX_tablefullandfinalsettlement_EmployeeId (EmployeeId)
);

CREATE TABLE tableprofessionaltaxslab (
    ProfessionalTaxSlabId INT NOT NULL AUTO_INCREMENT,
    State VARCHAR(100) NOT NULL,
    SalaryFrom DECIMAL(18,2) NOT NULL,
    SalaryTo DECIMAL(18,2) NULL,
    Month INT NULL,
    TaxAmount DECIMAL(18,2) NOT NULL,
    IsActive BIT NOT NULL,
    CreatedOn DATETIME NOT NULL,
    PRIMARY KEY (ProfessionalTaxSlabId)
);
