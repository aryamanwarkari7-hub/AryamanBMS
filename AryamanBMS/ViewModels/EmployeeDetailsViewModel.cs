using AryamanBMS.Models;

namespace AryamanBMS.ViewModels
{
    public class EmployeeDetailsViewModel
    {
        public EmployeeModel Employee { get; set; } = new();

        public List<EmployeeAcademicModel> Academics { get; set; }
            = new();

        public List<EmployeeDocumentModel> StatutoryDocuments { get; set; }
            = new();

        public List<EmployeeDocumentModel> JoiningDocuments { get; set; }
            = new();

        public List<EmployeePreviousEmploymentModel> PreviousEmployments
        { get; set; } = new();
    }
}