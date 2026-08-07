namespace AryamanBMS.ViewModels
{
    public class CalendarEventViewModel
    {
        public string Title { get; set; } = string.Empty;
        public DateTime Start { get; set; }
        public DateTime? End { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Color { get; set; } = "secondary";
        public string? Url { get; set; }
        public bool AllDay { get; set; }

        public string TextColor { get; set; } = "#ffffff";

        public int? Id { get; set; }
        public bool IsManual { get; set; }

    }
}