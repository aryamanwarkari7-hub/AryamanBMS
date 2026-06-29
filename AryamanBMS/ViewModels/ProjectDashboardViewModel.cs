using AryamanBMS.Models;

namespace AryamanBMS.ViewModels
{
    public class ProjectDashboardViewModel
    {
        public ProjectModel Project { get; set; } = null!;

        // Members
        public int ActiveMemberCount { get; set; }

        // Tasks
        public int TotalTaskCount { get; set; }
        public int NotStartedTaskCount { get; set; }
        public int InProgressTaskCount { get; set; }
        public int CompletedTaskCount { get; set; }
        public int OverdueTaskCount { get; set; }

        public decimal EstimatedHours { get; set; }
        public decimal ActualHours { get; set; }

        public decimal OverallProgress { get; set; }

        // Health
        public string ProjectHealth { get; set; } = "Good";

        public string ProjectHealthMessage { get; set; } =
            "Project is progressing normally.";

        // Risks
        public int OpenRiskCount { get; set; }
        public int CriticalRiskCount { get; set; }

        // Meetings
        public int UpcomingMeetingCount { get; set; }

        // Flow
        public string CurrentFlowStage { get; set; } = "Not Started";
        public string CurrentFlowStatus { get; set; } = "-";

        // Gantt
        public ProjectGanttViewModel Gantt { get; set; }
          = new ProjectGanttViewModel();
    }
}