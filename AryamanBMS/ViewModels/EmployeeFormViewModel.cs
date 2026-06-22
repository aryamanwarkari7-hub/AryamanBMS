using AryamanBMS.Models;

namespace AryamanBMS.ViewModels
{
    public class EmployeeFormViewModel
    {
        public EmployeeModel Employee { get; set; } = new();

        public List<EmployeeAcademicInputViewModel> Academics { get; set; }
            = new();
    }
}