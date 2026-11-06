namespace AryamanBMS.ViewModels
{
    public class BirthdayListItemViewModel
    {
        public int EmployeeId { get; set; }

        public string EmployeeCode { get; set; } = string.Empty;

        public string EmployeeName { get; set; } = string.Empty;

        public string? DepartmentName { get; set; }

        public string? DesignationName { get; set; }

        public string? ProfilePhotoPath { get; set; }

        public int Day { get; set; }

        public int Month { get; set; }

        public bool IsToday { get; set; }

        public int DaysUntil { get; set; }
    }
}
