namespace AryamanBMS.Models
{
    public class LetterModel
    {
        public int Id { get; set; }

        public string LetterNumber { get; set; }
            = string.Empty;

        public string LetterType { get; set; }
            = string.Empty;

        public int EmployeeId { get; set; }

        public EmployeeModel? Employee { get; set; }

        public string Subject { get; set; }
            = string.Empty;

        public string Body { get; set; }
            = string.Empty;

        public string? DocumentPath { get; set; }

        public string? IssuedBy { get; set; }

        public DateTime IssuedOn { get; set; }

        public bool IsActive { get; set; } = true;
    }
}