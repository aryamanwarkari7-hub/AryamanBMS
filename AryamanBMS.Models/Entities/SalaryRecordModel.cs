using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AryamanBMS.Models
{
    public class SalaryRecordModel
    {
        public int Id { get; set; }

        public int EmployeeId { get; set; }

        public EmployeeModel? Employee { get; set; }

        public int Month { get; set; }

        public int Year { get; set; }

        public decimal ActualSalary { get; set; }

        public decimal PayDays { get; set; }

        public decimal StandardMonthlySalary { get; set; }

        public decimal PayrollDivisor { get; set; }

        public decimal EligibleEmploymentDays { get; set; }

        public decimal PresentDaysValue { get; set; }

        public decimal PaidLeaveDays { get; set; }

        public decimal UnpaidLeaveDays { get; set; }

        public decimal LeaveWithoutPayDays { get; set; }

        public decimal WeeklyOffs { get; set; }

        public decimal Holidays { get; set; }

        public decimal OnDutyDays { get; set; }

        public decimal PerDaySalary { get; set; }

        public decimal ProratedSalary { get; set; }

        public decimal BasicSalary { get; set; }

        public decimal HRA { get; set; }

        public decimal Conveyance { get; set; }

        public decimal MedicalAllowance { get; set; }

        public decimal EducationAllowance { get; set; }

        public decimal SpecialAllowance { get; set; }

        public decimal GrossSalary { get; set; }

        public decimal TotalEarnings { get; set; }

        public decimal GrossMinusConveyance { get; set; }

        public decimal PfDeduction { get; set; }

        public decimal EsicDeduction { get; set; }

        public decimal ProfessionalTax { get; set; }

        public decimal Advance { get; set; }

        public decimal TotalDeductions { get; set; }

        public decimal NetSalary { get; set; }

        public decimal EmployerPf { get; set; }

        public decimal EmployerEsic { get; set; }

        public decimal CTC { get; set; }

        public decimal DA { get; set; }

        public decimal OtherAllowances { get; set; }

        public decimal TdsDeduction { get; set; }

        public decimal OtherDeductions { get; set; }

        public bool IsPfApplicable { get; set; }

        public decimal PfWage { get; set; }

        public decimal EmployeePfRate { get; set; }

        public decimal EmployerPfRate { get; set; }

        public decimal PensionComponent { get; set; }

        [StringLength(500)]
        public string? PfNonApplicabilityReason { get; set; }

        public bool IsEsicApplicable { get; set; }

        public decimal EsicWage { get; set; }

        public decimal EmployeeEsicRate { get; set; }

        public decimal EmployerEsicRate { get; set; }

        [StringLength(500)]
        public string? EsicNonApplicabilityReason { get; set; }

        [StringLength(100)]
        public string? ProfessionalTaxState { get; set; }

        [StringLength(100)]
        public string? ProfessionalTaxSlab { get; set; }

        [StringLength(500)]
        public string? ProfessionalTaxExemptionReason { get; set; }

        [StringLength(30)]
        public string? TaxRegime { get; set; }

        public decimal EstimatedAnnualIncome { get; set; }

        public decimal PreviousEmployerIncome { get; set; }

        public decimal TaxExemptions { get; set; }

        public decimal ChapterSixDeductions { get; set; }

        public decimal OtherIncomeDeclared { get; set; }

        public decimal TaxAlreadyDeducted { get; set; }

        public decimal AnnualTaxLiability { get; set; }

        [StringLength(30)]
        public string? DeclarationStatus { get; set; }

        [StringLength(150)]
        public string? Form12BBReference { get; set; }

        [StringLength(150)]
        public string? Form16Reference { get; set; }

        public decimal Bonus { get; set; }

        public decimal Incentive { get; set; }

        public decimal OvertimeHours { get; set; }

        public decimal OvertimeRate { get; set; }

        public decimal OvertimeAmount { get; set; }

        public decimal Arrears { get; set; }

        public decimal Reimbursement { get; set; }

        public decimal LeaveEncashment { get; set; }

        public decimal OneTimeAdjustment { get; set; }

        public decimal BonusProvision { get; set; }

        public decimal GratuityProvision { get; set; }

        public decimal InsuranceBenefit { get; set; }

        public decimal OtherEmployerBenefits { get; set; }

        public decimal MonthlyCTC { get; set; }

        [StringLength(30)]
        public string PaymentStatus { get; set; } = "Pending";

        [StringLength(30)]
        public string PayrollStatus { get; set; } = "Draft";

        public DateTime? PaidOn { get; set; }

        [StringLength(450)]
        public string? PaidByUserId { get; set; }

        [StringLength(500)]
        public string? PaymentFailureReason { get; set; }

        [StringLength(500)]
        public string? PaymentReversalReason { get; set; }

        public DateTime? RetriedOn { get; set; }

        [StringLength(450)]
        public string? RetriedByUserId { get; set; }

        [StringLength(450)]
        public string? GeneratedByUserId { get; set; }

        public DateTime? GeneratedOn { get; set; }

        [StringLength(450)]
        public string? VerifiedByUserId { get; set; }

        public DateTime? VerifiedOn { get; set; }

        [StringLength(450)]
        public string? FinalizedByUserId { get; set; }

        public DateTime? FinalizedOn { get; set; }

        [StringLength(450)]
        public string? ReopenedByUserId { get; set; }

        public DateTime? ReopenedOn { get; set; }

        [StringLength(500)]
        public string? ReopenReason { get; set; }

        public bool IsPayslipReleased { get; set; }

        [StringLength(450)]
        public string? PayslipReleasedByUserId { get; set; }

        public DateTime? PayslipReleasedOn { get; set; }

        public DateTime? EmployeeViewedPayslipOn { get; set; }

        public int PresentDays { get; set; }

        public int LeaveDays { get; set; }

        public int AbsentDays { get; set; }

        public int? SalaryImportBatchId { get; set; }

        public string? SourceFileName { get; set; }

        [StringLength(450)]
        public string? ImportedByUserId { get; set; }

        public DateTime ImportedOn { get; set; } = DateTime.Now;

        [StringLength(450)]
        public string? CreatedByUserId { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        [StringLength(450)]
        public string? UpdatedByUserId { get; set; }

        public DateTime? UpdatedOn { get; set; }

        [StringLength(150)]
        public string? JournalEntryReference { get; set; }

        public DateTime? AccountingPostingDate { get; set; }

        public string? Remark { get; set; }

        [ForeignKey(nameof(SalaryImportBatchId))]
        public SalaryImportBatchModel? SalaryImportBatch { get; set; }
    }
}
