namespace AryamanBMS.ViewModels
{
    public class ProjectOverviewDashboardViewModel
    {
        public int TotalProjects { get; set; }

        public int ActiveProjects { get; set; }

        public int CompletedProjects { get; set; }

        public int OnHoldProjects { get; set; }

        public int OverdueProjects { get; set; }

        public int TotalTasks { get; set; }

        public int OpenTasks { get; set; }

        public int OverdueTasks { get; set; }

        public int CompletedTasks { get; set; }

        public int OpenRisks { get; set; }

        public int CriticalRisks { get; set; }

        public int UpcomingMeetings { get; set; }

        public int PendingMilestones { get; set; }

        public decimal AverageProgress { get; set; }

        public decimal TotalBudget { get; set; }

        public List<ProjectOverviewBucket> StatusBuckets { get; set; } = new();

        public List<ProjectOverviewBucket> PriorityBuckets { get; set; } = new();

        public List<ProjectOverviewBucket> TaskStatusBuckets { get; set; } = new();

        public List<ProjectOverviewListItem> OverdueProjectList { get; set; } = new();

        public List<ProjectOverviewListItem> OverdueTaskList { get; set; } = new();

        public List<ProjectOverviewListItem> CriticalRiskList { get; set; } = new();

        public List<ProjectOverviewListItem> UpcomingMeetingList { get; set; } = new();
    }

    public class ProjectOverviewBucket
    {
        public string Label { get; set; } = string.Empty;

        public int Count { get; set; }

        public decimal Percent { get; set; }

        public string CssClass { get; set; } = "bucket-info";
    }

    public class ProjectOverviewListItem
    {
        public int Id { get; set; }

        public int? ProjectId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Subtitle { get; set; }

        public string? Meta { get; set; }

        public string? Badge { get; set; }
    }
}