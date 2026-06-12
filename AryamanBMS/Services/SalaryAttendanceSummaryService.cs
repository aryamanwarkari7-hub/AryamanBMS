using AryamanBMS.Repositories.Interfaces;
using AryamanBMS.Services.Interfaces;
using AryamanBMS.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Services
{
    public class SalaryAttendanceSummaryService : ISalaryAttendanceSummaryService
    {
        private readonly IEmployeeRepository _employeeRepository;

        private readonly IAttendanceRepository _attendanceRepository;

        private readonly ILeaveApplicationRepository _leaveApplicationRepository;

        public SalaryAttendanceSummaryService(
            IEmployeeRepository employeeRepository,
            IAttendanceRepository attendanceRepository,
            ILeaveApplicationRepository leaveApplicationRepository)
        {
            _employeeRepository = employeeRepository;
            _attendanceRepository = attendanceRepository;
            _leaveApplicationRepository = leaveApplicationRepository;
        }

        public async Task<List<AttendanceSummaryViewModel>> GetMonthlySummaryAsync(
            int month,
            int year)
        {
            int totalDays = DateTime.DaysInMonth(year, month);

            var startDate = new DateTime(year, month, 1);

            var endDate = new DateTime(year, month, totalDays);

            var employees = await _employeeRepository.Employees
                .Where(e => e.IsActive)
                .OrderBy(e => e.EmployeeCode)
                .ToListAsync();

            var attendanceRecords = await _attendanceRepository.Attendances
                .Where(a =>
                    a.AttendanceDate >= startDate &&
                    a.AttendanceDate <= endDate)
                .ToListAsync();

            var approvedLeaves = await _leaveApplicationRepository.LeaveApplications
                .Include(l => l.LeaveType)
                .Where(l =>
                    l.Status == "Approved" &&
                    l.FromDate <= endDate &&
                    l.ToDate >= startDate)
                .ToListAsync();

            var summaries = new List<AttendanceSummaryViewModel>();

            foreach (var employee in employees)
            {
                var employeeAttendance = attendanceRecords
                    .Where(a => a.EmployeeId == employee.Id)
                    .ToList();

                int presentCount = employeeAttendance
    .Count(a => IsStatus(a.Status, "P", "Present"));

                int markedAbsentCount = employeeAttendance
                    .Count(a => IsStatus(a.Status, "A", "Absent"));

                int leaveCount = employeeAttendance
                    .Count(a => IsStatus(a.Status, "L", "Leave"));

                int holidayCount = employeeAttendance
                    .Count(a => IsStatus(a.Status, "H", "Holiday"));

                int weekOffCount = employeeAttendance
                    .Count(a => IsStatus(a.Status, "WO", "Week Off", "WeekOff", "Weekly Off"));

                int onDutyCount = employeeAttendance
                    .Count(a => IsStatus(a.Status, "OD", "On Duty", "OnDuty"));

                var employeeApprovedLeaves = approvedLeaves
                    .Where(l => l.EmployeeId == employee.Id)
                    .ToList();

                int paidLeaveCount = employeeApprovedLeaves
                    .Where(l => l.LeaveType != null && l.LeaveType.IsPaidLeave)
                    .Sum(l => CountOverlapDays(
                        l.FromDate,
                        l.ToDate,
                        startDate,
                        endDate));

                int unpaidLeaveCount = employeeApprovedLeaves
                    .Where(l => l.LeaveType != null && !l.LeaveType.IsPaidLeave)
                    .Sum(l => CountOverlapDays(
                        l.FromDate,
                        l.ToDate,
                        startDate,
                        endDate));

                int accountedDays =
                     presentCount
                     + onDutyCount
                     + holidayCount
                     + weekOffCount
                     + paidLeaveCount
                     + unpaidLeaveCount
                     + markedAbsentCount;

                int missingDays = totalDays - accountedDays;

                if (missingDays < 0)
                {
                    missingDays = 0;
                }

                int absentCount =
                        markedAbsentCount
                        + missingDays
                        + unpaidLeaveCount;

                int payDays =
                    presentCount
                    + onDutyCount
                    + holidayCount
                    + weekOffCount
                    + paidLeaveCount;

                int workingDays =
                    totalDays - holidayCount - weekOffCount;

                decimal attendancePercentage =
                    workingDays == 0
                        ? 0
                        : Math.Round(
                            ((decimal)(presentCount + onDutyCount)
                            / workingDays) * 100,
                            2);

                summaries.Add(new AttendanceSummaryViewModel
                {
                    EmployeeId = employee.Id,

                    EmployeeCode = employee.EmployeeCode ?? string.Empty,

                    EmployeeName = employee.FullName,

                    Month = month,

                    Year = year,

                    PresentCount = presentCount,

                    AbsentCount = absentCount,

                    MarkedAbsentCount = markedAbsentCount,

                    MissingDays = missingDays,

                    LeaveCount = leaveCount,

                    PaidLeaveCount = paidLeaveCount,

                    UnpaidLeaveCount = unpaidLeaveCount,

                    HolidayCount = holidayCount,

                    WeekOffCount = weekOffCount,

                    OnDutyCount = onDutyCount,

                    TotalDays = totalDays,

                    PayDays = payDays,

                    AttendancePercentage = attendancePercentage
                });
            }

            return summaries;
        }

        private int CountOverlapDays(
            DateTime leaveFrom,
            DateTime leaveTo,
            DateTime monthStart,
            DateTime monthEnd)
        {
            var fromDate = leaveFrom.Date > monthStart.Date
                ? leaveFrom.Date
                : monthStart.Date;

            var toDate = leaveTo.Date < monthEnd.Date
                ? leaveTo.Date
                : monthEnd.Date;

            if (fromDate > toDate)
            {
                return 0;
            }

            return (toDate - fromDate).Days + 1;
        }

        private bool IsStatus(  string? status, params string[] validStatuses)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return false;
            }

            return validStatuses.Any(x =>
                string.Equals(
                    status.Trim(),
                    x,
                    StringComparison.OrdinalIgnoreCase));
        }
    }
}