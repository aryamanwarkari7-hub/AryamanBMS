using AryamanBMS.Models;

namespace AryamanBMS.ViewModels
{
    public class EmployeeFormViewModel
    {
        public EmployeeModel Employee { get; set; } = new();

        public List<EmployeeAcademicInputViewModel> Academics { get; set; }
            = new();

        public List<EmployeeDocumentInputViewModel> StatutoryDocuments
        { get; set; } = new();

        public List<EmployeeDocumentInputViewModel> JoiningDocuments
        { get; set; } = new();

        public List<int> RemovedAcademicIds { get; set; } = new();

        public List<EmployeePreviousEmploymentInputViewModel> PreviousEmployments
        { get; set; } = new();

        public List<int> RemovedPreviousEmploymentIds { get; set; } = new();
    }
}