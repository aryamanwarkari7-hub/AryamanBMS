namespace AryamanBMS.ViewModels
{
    public class ProjectGanttViewModel
    {
        public string ProjectManagerName { get; set; } = "Not Assigned";
        public DateTime TimelineStart { get; set; }

        public DateTime TimelineEnd { get; set; }

        public int TotalTimelineDays { get; set; }

        public List<ProjectGanttTaskViewModel> Tasks { get; set; } = new();

        public decimal? TodayPositionPercent { get; set; }

        public List<ProjectGanttDateViewModel> TimelineDates { get; set; }
            = new();
    }

    public class ProjectGanttTaskViewModel
    {

        public int? AssignedEmployeeId { get; set; }

        public string AssignedEmployeeName { get; set; } = "Unassigned";
        public int TaskId { get; set; }

        public string TaskCode { get; set; } = string.Empty;

        public string TaskTitle { get; set; } = string.Empty;

        public DateTime StartDate { get; set; }

        public DateTime DueDate { get; set; }

        public string Status { get; set; } = string.Empty;

        public int ProgressPercent { get; set; }

        public int StartOffsetDays { get; set; }

        public int DurationDays { get; set; }

        public decimal LeftPercent { get; set; }

        public decimal WidthPercent { get; set; }

        public bool IsOverdue { get; set; }

        public bool IsMilestone { get; set; }

        public int DisplayProgressPercent { get; set; }

        public int OverdueDays { get; set; }


    }

    public class ProjectGanttDateViewModel
    {
        public DateTime Date { get; set; }

        public decimal PositionPercent { get; set; }
    }
}