namespace AryamanBMS.ViewModels
{
    public class LeaveDashboardViewModel
    {
        public int TotalApplications { get; set; }

        public int PendingApplications { get; set; }

        public int ApprovedApplications { get; set; }

        public int RejectedApplications { get; set; }

        public int CancelledApplications { get; set; }
    }
}