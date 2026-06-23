using AryamanBMS.Models;

namespace AryamanBMS.ViewModels
{
    public class ProjectMeetingDetailsViewModel
    {
        public ProjectMeetingModel Meeting { get; set; }
            = new();

        public IEnumerable<EmployeeModel> Employees { get; set; }
            = Enumerable.Empty<EmployeeModel>();
    }
}