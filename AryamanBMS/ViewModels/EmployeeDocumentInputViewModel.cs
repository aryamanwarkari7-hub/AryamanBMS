using AryamanBMS.Models;

namespace AryamanBMS.ViewModels
{
    public class EmployeeDocumentInputViewModel
    {
        public string DocumentType { get; set; } = string.Empty;

        public IFormFile? File { get; set; }

        public EmployeeDocumentModel? ExistingDocument { get; set; }
    }
}