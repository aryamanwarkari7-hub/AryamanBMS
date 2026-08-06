using AryamanBMS.Repositories.Interfaces;
using AryamanBMS.Services.Interface;
using AryamanBMS.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Services
{
    public class SalaryAttendanceSummaryService : ISalaryAttendanceSummaryService
    {
        private readonly IEmployeeRepository _employeeRepository;

        private readonly IAttendanceRepository _attendanceRepository;

        private readonly ILeaveApplicationRepository _leaveApplicationRepository;
        private readonly IConfiguration _configuration;

        public SalaryAttendanceSummaryService(
            IEmployeeRepository employeeRepository,
            IAttendanceRepository attendanceRepository,
            ILeaveApplicationRepository leaveApplicationRepository,
            IConfiguration configuration)
        {
            _employeeRepository = employeeRepository;
            _attendanceRepository = attendanceRepository;
            _leaveApplicationRepository = leaveApplicationRepository;
            _configuration = configuration;
        }

        public async Task<List<AttendanceSummaryViewModel>> GetMonthlySummaryAsync(
            int month,
            int year)
        {
            int totalDays = DateTime.DaysInMonth(year, month);

            var startDate = new DateTime(year, month, 1);

            var endDate = new DateTime(year, month, totalDays);

            var employees = await _employeeRepository.Employees
                .Where(e =>
                    e.JoiningDate.Date <= endDate &&
                    (
                        e.IsActive ||
                        (
                            e.LastWorkingDate.HasValue &&
                            e.LastWorkingDate.Value.Date >= startDate
                        )
                    ))
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
            var weeklyOffDays = GetWeeklyOffDays();
            var officeHolidays = GetOfficeHolidays(startDate, endDate);

            foreach (var employee in employees)
            {
                DateTime eligibleStart =
                    employee.JoiningDate.Date > startDate
                        ? employee.JoiningDate.Date
                        : startDate;

                DateTime eligibleEnd =
                    employee.LastWorkingDate.HasValue &&
                    employee.LastWorkingDate.Value.Date < endDate
                        ? employee.LastWorkingDate.Value.Date
                        : endDate;

                if (eligibleStart > eligibleEnd)
                {
                    continue;
                }

                int eligibleDays =
                    (eligibleEnd - eligibleStart).Days + 1;

                var employeeAttendance = attendanceRecords
                    .Where(a =>
                        a.EmployeeId == employee.Id &&
                        a.AttendanceDate.Date >= eligibleStart &&
                        a.AttendanceDate.Date <= eligibleEnd)
                    .GroupBy(a => a.AttendanceDate.Date)
                    .ToDictionary(
                        g => g.Key,
                        g => g.OrderByDescending(a => a.Id).First());

                var employeeApprovedLeaves = approvedLeaves
                    .Where(l => l.EmployeeId == employee.Id)
                    .ToList();
                int presentCount = 0;
                int markedAbsentCount = 0;
                int leaveCount = 0;
                int paidLeaveCount = 0;
                int unpaidLeaveCount = 0;
                int holidayCount = 0;
                int weekOffCount = 0;
                int onDutyCount = 0;
                int missingDays = 0;

                for (var date = eligibleStart.Date;
                     date <= eligibleEnd.Date;
                     date = date.AddDays(1))
                {
                    employeeAttendance.TryGetValue(date, out var attendance);

                    if (IsStatus(attendance?.Status, "P", "Present"))
                    {
                        presentCount++;
                        continue;
                    }

                    if (IsStatus(attendance?.Status, "OD", "On Duty", "OnDuty"))
                    {
                        onDutyCount++;
                        continue;
                    }

                    if (IsStatus(attendance?.Status, "A", "Absent"))
                    {
                        markedAbsentCount++;
                        continue;
                    }

                    if (IsStatus(attendance?.Status, "H", "Holiday") ||
                        IsOfficeHoliday(date, officeHolidays))
                    {
                        holidayCount++;
                        continue;
                    }

                    if (IsStatus(attendance?.Status, "WO", "Week Off", "WeekOff", "Weekly Off") ||
                        IsWeeklyOff(date, weeklyOffDays))
                    {
                        weekOffCount++;
                        continue;
                    }

                    var approvedLeave = employeeApprovedLeaves
                        .FirstOrDefault(l =>
                            l.FromDate.Date <= date &&
                            l.ToDate.Date >= date);

                    if (approvedLeave != null ||
                        IsStatus(attendance?.Status, "L", "Leave"))
                    {
                        leaveCount++;

                        if (approvedLeave?.LeaveType?.IsPaidLeave == true)
                        {
                            paidLeaveCount++;
                        }
                        else
                        {
                            unpaidLeaveCount++;
                        }

                        continue;
                    }

                    missingDays++;
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
                    eligibleDays - holidayCount - weekOffCount;

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

                    TotalDays = eligibleDays,

                    PayDays = payDays,

                    AttendancePercentage = attendancePercentage
                });
            }

            return summaries;
        }

        private HashSet<DayOfWeek> GetWeeklyOffDays()
        {
            var configuredDays =
                _configuration
                    .GetSection("Attendance:WeeklyOffDays")
                    .Get<string[]>();

            if (configuredDays == null || configuredDays.Length == 0)
            {
                return new HashSet<DayOfWeek>
                {
                    DayOfWeek.Sunday
                };
            }

            var weeklyOffDays = new HashSet<DayOfWeek>();

            foreach (var configuredDay in configuredDays)
            {
                if (Enum.TryParse(
                        configuredDay,
                        ignoreCase: true,
                        out DayOfWeek dayOfWeek))
                {
                    weeklyOffDays.Add(dayOfWeek);
                }
            }

            if (weeklyOffDays.Count == 0)
            {
                weeklyOffDays.Add(DayOfWeek.Sunday);
            }

            return weeklyOffDays;
        }

        private HashSet<DateTime> GetOfficeHolidays(
            DateTime startDate,
            DateTime endDate)
        {
            var configuredHolidays =
                _configuration
                    .GetSection("Attendance:OfficeHolidays")
                    .Get<string[]>();

            var officeHolidays = new HashSet<DateTime>();

            if (configuredHolidays == null)
            {
                return officeHolidays;
            }

            foreach (var configuredHoliday in configuredHolidays)
            {
                if (DateTime.TryParse(configuredHoliday, out var holiday))
                {
                    var holidayDate = holiday.Date;

                    if (holidayDate >= startDate.Date &&
                        holidayDate <= endDate.Date)
                    {
                        officeHolidays.Add(holidayDate);
                    }
                }
            }

            return officeHolidays;
        }

        private bool IsWeeklyOff(
            DateTime date,
            HashSet<DayOfWeek> weeklyOffDays)
        {
            return weeklyOffDays.Contains(date.DayOfWeek);
        }

        private bool IsOfficeHoliday(
            DateTime date,
            HashSet<DateTime> officeHolidays)
        {
            return officeHolidays.Contains(date.Date);
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
