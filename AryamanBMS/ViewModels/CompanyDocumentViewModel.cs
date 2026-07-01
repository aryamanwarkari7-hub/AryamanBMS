using AryamanBMS.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AryamanBMS.ViewModels;

public class CompanyDocumentViewModel
{
    public CompanyDocumentModel Document { get; set; } = new();

    public IFormFile? UploadFile { get; set; }

    public IEnumerable<SelectListItem> Categories { get; set; }
        = Enumerable.Empty<SelectListItem>();
}