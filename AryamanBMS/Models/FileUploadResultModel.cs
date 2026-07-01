namespace AryamanBMS.Models
{
    public class FileUploadResult
    {
        public bool Success { get; set; }

        public string OriginalFileName { get; set; } = string.Empty;

        public string StoredFileName { get; set; } = string.Empty;

        public string RelativePath { get; set; } = string.Empty;

        public string PhysicalPath { get; set; } = string.Empty;

        public string FileExtension { get; set; } = string.Empty;

        public string ContentType { get; set; } = string.Empty;

        public long FileSize { get; set; }

        public string ErrorMessage { get; set; } = string.Empty;
    }
}