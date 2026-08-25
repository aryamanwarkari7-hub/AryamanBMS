using System.ComponentModel.DataAnnotations.Schema;

namespace AryamanBMS.Models
{
    public class AttendanceModel
    {
        public int Id { get; set; }

        public int EmployeeId { get; set; }

        public DateTime AttendanceDate { get; set; }

        public string Status { get; set; } = "P";

        public decimal AttendanceValue { get; set; } = 1m;

        public DateTime? CheckInTime { get; set; }

        public DateTime? CheckOutTime { get; set; }

        public string? LocationType { get; set; }

        // Off-Day work access
        public bool IsOffDayWork { get; set; } = false;

        public string? OffDayType { get; set; }


        public string? Remarks { get; set; }

        public DateTime CreatedOn { get; set; }

        public EmployeeModel? Employee { get; set; }

        // Late checkout correction request
        public DateTime? RequestedCheckOutTime { get; set; }

        public string? LateCheckoutReason { get; set; }

        public DateTime? LateCheckoutRequestedOn { get; set; }

        public string? LateCheckoutRequestStatus { get; set; }
        // Pending | Approved | Rejected

        public DateTime? LateCheckoutResolvedOn { get; set; }

        public string? LateCheckoutResolvedByUserId { get; set; }

        public string? LateCheckoutResolutionNote { get; set; }


        [NotMapped]
        public double WorkingHours
        {
            get
            {
                if (CheckInTime.HasValue && CheckOutTime.HasValue)
                {
                    return (CheckOutTime.Value - CheckInTime.Value).TotalHours;
                }

                return 0;
            }
        }
    }
}
