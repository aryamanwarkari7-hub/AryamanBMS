namespace AryamanBMS.ViewModels
{
    public class SalaryImportResult
    {
        public int ImportedCount { get; set; }

        public List<string> Errors { get; set; } = new();

        public bool HasErrors => Errors.Any();

        public string Message
        {
            get
            {
                if (HasErrors)
                {
                    return $"{ImportedCount} records imported with {Errors.Count} errors.";
                }

                return $"{ImportedCount} salary records imported successfully.";
            }
        }
    }
}