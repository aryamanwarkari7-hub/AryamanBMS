using AryamanBMS.Models;

namespace AryamanBMS.Services.Interface
{
    public class EmployeeDocumentService : IEmployeeDocumentService
    {
        private readonly IWebHostEnvironment _environment;

        private const long MaximumFileSize = 5 * 1024 * 1024;

        private static readonly string[] AllowedExtensions =
        {
            ".pdf", ".jpg", ".jpeg", ".png"
        };

        private static readonly Dictionary<string, string> AllowedContentTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = "application/pdf",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png"
        };

        public EmployeeDocumentService(
            IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<EmployeeDocumentModel> SaveAsync(
            IFormFile file,
            string employeeCode,
            string documentType,
            string? uploadedBy)
        {
            if (file == null || file.Length == 0)
            {
                throw new InvalidOperationException(
                    "Uploaded file is empty.");
            }

            if (file.Length > MaximumFileSize)
            {
                throw new InvalidOperationException(
                    "File size cannot exceed 5 MB.");
            }

            string extension =
                Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!AllowedExtensions.Contains(extension))
            {
                throw new InvalidOperationException(
                    "Only PDF, JPG, JPEG and PNG files are allowed.");
            }

            if (!AllowedContentTypes.TryGetValue(
                 extension,
                 out string? expectedContentType) ||
             !string.Equals(
                 file.ContentType,
                 expectedContentType,
                 StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Uploaded file type does not match the selected file.");
            }

            string safeEmployeeCode = string.Concat(
                employeeCode.Where(x =>
                    char.IsLetterOrDigit(x) ||
                    x == '-' ||
                    x == '_'));

            string storedFileName =
                $"{Guid.NewGuid():N}{extension}";

            string relativePath = Path.Combine(
                "EmployeeDocuments",
                safeEmployeeCode,
                storedFileName);

            string absolutePath = Path.Combine(
                _environment.ContentRootPath,
                "App_Data",
                relativePath);

            Directory.CreateDirectory(
                Path.GetDirectoryName(absolutePath)!);

            await using var stream =
                new FileStream(
                    absolutePath,
                    FileMode.CreateNew);

            await file.CopyToAsync(stream);

            return new EmployeeDocumentModel
            {
                DocumentType = documentType,
                OriginalFileName =
                    Path.GetFileName(file.FileName),
                StoredFileName = storedFileName,
                StoragePath = relativePath,
                ContentType = expectedContentType,
                FileSize = file.Length,
                UploadedOn = DateTime.Now,
                UploadedBy = uploadedBy
            };
        }

        public Task DeleteAsync(string storagePath)
        {
            string absolutePath =
                GetAbsolutePath(storagePath);

            if (File.Exists(absolutePath))
            {
                File.Delete(absolutePath);
            }

            return Task.CompletedTask;
        }

        public string GetAbsolutePath(string storagePath)
        {
            string appDataRoot =
                Path.GetFullPath(
                    Path.Combine(
                        _environment.ContentRootPath,
                        "App_Data"));

            string appDataRootWithSeparator =
                Path.EndsInDirectorySeparator(appDataRoot)
                    ? appDataRoot
                    : appDataRoot + Path.DirectorySeparatorChar;

            string absolutePath =
                Path.GetFullPath(
                    Path.Combine(
                        appDataRoot,
                        storagePath));

            if (!absolutePath.StartsWith(
                appDataRootWithSeparator,
                StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Invalid document path.");
            }

            return absolutePath;
        }
    }
}