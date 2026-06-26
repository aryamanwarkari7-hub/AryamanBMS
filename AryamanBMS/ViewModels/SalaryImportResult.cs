namespace AryamanBMS.ViewModels
{
    public class SalaryImportResult
    {
        public int ImportedCount { get; set; }

        public List<string> Errors { get; set; } = new();

        public bool HasErrors => Errors.Any();

        public int SkippedPaidCount { get; set; }

        public string Message
        {
            get
            {
                string message =
                    $"{ImportedCount} salary record(s) imported successfully.";

                if (SkippedPaidCount > 0)
                {
                    message +=
                        $" {SkippedPaidCount} paid record(s) skipped.";
                }

                return message;
            }
        }
    }
}