namespace AryamanBMS.Models
{
    public class WorkingDayOptions
    {
        public List<string> OfficeHolidays { get; set; } = [];

        public List<DayOfWeek> WeeklyOffDays { get; set; } = [];

        public List<int> WorkingSaturdayNumbers { get; set; } = [1, 3, 5];
    }
}