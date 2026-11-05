using AryamanBMS.Models;

namespace AryamanBMS.ViewModels
{
    public class EmployeeDocumentInputViewModel
    {
        public string DocumentType { get; set; } = string.Empty;

        public List<IFormFile>? Files { get; set; } = new();

        public List<EmployeeDocumentModel> ExistingDocuments
        { get; set; } = new();
    }
}