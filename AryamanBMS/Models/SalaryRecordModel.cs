namespace AryamanBMS.Models
{
    public class SalaryRecordModel
    {
        public int Id { get; set; }

        public int EmployeeId { get; set; }

        public EmployeeModel? Employee { get; set; }

        public int Month { get; set; }

        public int Year { get; set; }

        public decimal BasicSalary { get; set; }

        public decimal HRA { get; set; }

        public decimal DA { get; set; }

        public decimal OtherAllowances { get; set; }

        public decimal GrossSalary { get; set; }

        public decimal PfDeduction { get; set; }

        public decimal EsicDeduction { get; set; }

        public decimal TdsDeduction { get; set; }

        public decimal OtherDeductions { get; set; }

        public decimal NetSalary { get; set; }

        public string PaymentStatus { get; set; }
            = "Pending";

        public DateTime? PaidOn { get; set; }

        public int PresentDays { get; set; }

        public int LeaveDays { get; set; }

        public int AbsentDays { get; set; }
    }
}