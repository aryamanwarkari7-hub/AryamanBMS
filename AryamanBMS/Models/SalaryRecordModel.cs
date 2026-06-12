namespace AryamanBMS.Models
{
    public class SalaryRecordModel
    {
        public int Id { get; set; }

        public int EmployeeId { get; set; }

        public EmployeeModel? Employee { get; set; }

        public int Month { get; set; }

        public int Year { get; set; }

        // Excel source values
        public decimal ActualSalary { get; set; }

        public decimal PayDays { get; set; }

        // Earnings
        public decimal BasicSalary { get; set; }

        public decimal HRA { get; set; }

        public decimal Conveyance { get; set; }

        public decimal MedicalAllowance { get; set; }

        public decimal EducationAllowance { get; set; }

        public decimal SpecialAllowance { get; set; }

        public decimal GrossSalary { get; set; }

        public decimal TotalEarnings { get; set; }

        public decimal GrossMinusConveyance { get; set; }

        // Deductions
        public decimal PfDeduction { get; set; }

        public decimal EsicDeduction { get; set; }

        public decimal ProfessionalTax { get; set; }

        public decimal Advance { get; set; }

        public decimal TotalDeductions { get; set; }

        public decimal NetSalary { get; set; }

        // Employer contribution
        public decimal EmployerPf { get; set; }

        public decimal EmployerEsic { get; set; }

        public decimal CTC { get; set; }

        // Existing fields kept temporarily
        public decimal DA { get; set; }

        public decimal OtherAllowances { get; set; }

        public decimal TdsDeduction { get; set; }

        public decimal OtherDeductions { get; set; }

        // Payment tracking
        public string PaymentStatus { get; set; } = "Pending";

        public DateTime? PaidOn { get; set; }

        // Attendance summary
        public int PresentDays { get; set; }

        public int LeaveDays { get; set; }

        public int AbsentDays { get; set; }

        // Import tracking
        public string? SourceFileName { get; set; }

        public DateTime ImportedOn { get; set; } = DateTime.Now;

        public string? Remark { get; set; }
    }
}