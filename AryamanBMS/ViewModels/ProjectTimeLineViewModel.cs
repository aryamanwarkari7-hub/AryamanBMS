using AryamanBMS.Models;

namespace AryamanBMS.ViewModels
{
    public class ProjectTimelineViewModel
    {
        public ProjectModel Project { get; set; } = null!;

        public List<ProjectTimelineModel> Events { get; set; }
            = new();

        public string ProjectManagerName { get; set; }
            = "Not Assigned";

        public string? SelectedEventType { get; set; }

        public string SortOrder { get; set; }
            = "oldest";

        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }

        public List<string> EventTypes { get; set; }
            = new();
    }
}