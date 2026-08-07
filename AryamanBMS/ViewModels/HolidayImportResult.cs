namespace AryamanBMS.ViewModels
{
    public class HolidayImportResult
    {
        public int AddedCount { get; set; }

        public int UpdatedCount { get; set; }

        public int SkippedCount { get; set; }

        public List<string> Errors { get; set; } = new();

        public bool HasErrors => Errors.Any();

        public string Message =>
            $"{AddedCount} holiday(s) added, {UpdatedCount} updated, {SkippedCount} skipped.";
    }
}