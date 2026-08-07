namespace AryamanBMS.ViewModels
{
    public class CalendarIndexViewModel
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public DateTime MonthStart { get; set; }
        public DateTime MonthEnd { get; set; }
        public List<CalendarEventViewModel> Events { get; set; } = new();
    }
}