using AryamanBMS.Repositories.Interfaces;
using AryamanBMS.Services.Interface;
using AryamanBMS.ViewModels;
using AryamanBMS.Data;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Services
{
    public class SalaryAttendanceSummaryService : ISalaryAttendanceSummaryService
    {
        private readonly IEmployeeRepository _employeeRepository;

        private readonly IAttendanceRepository _attendanceRepository;

        private readonly ILeaveApplicationRepository _leaveApplicationRepository;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public SalaryAttendanceSummaryService(
            IEmployeeRepository employeeRepository,
            IAttendanceRepository attendanceRepository,
            ILeaveApplicationRepository leaveApplicationRepository,
            ApplicationDbContext context,
            IConfiguration configuration)
        {
            _employeeRepository = employeeRepository;
            _attendanceRepository = attendanceRepository;
            _leaveApplicationRepository = leaveApplicationRepository;
            _context = context;
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
            var officeHolidays = await GetOfficeHolidaysAsync(startDate, endDate);

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

                decimal eligibleDays =
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
                decimal presentCount = 0;
                decimal markedAbsentCount = 0;
                decimal leaveCount = 0;
                decimal paidLeaveCount = 0;
                decimal unpaidLeaveCount = 0;
                decimal holidayCount = 0;
                decimal weekOffCount = 0;
                decimal onDutyCount = 0;
                decimal missingDays = 0;

                for (var date = eligibleStart.Date;
                     date <= eligibleEnd.Date;
                     date = date.AddDays(1))
                {
                    employeeAttendance.TryGetValue(date, out var attendance);
                    decimal attendanceValue =
                        NormalizeDayValue(attendance?.AttendanceValue ?? 1m);

                    if (IsStatus(attendance?.Status, "P", "Present"))
                    {
                        presentCount += attendanceValue;
                        continue;
                    }

                    if (IsStatus(attendance?.Status, "OD", "On Duty", "OnDuty"))
                    {
                        onDutyCount += attendanceValue;
                        continue;
                    }

                    if (IsStatus(attendance?.Status, "A", "Absent"))
                    {
                        markedAbsentCount += attendanceValue;

                        if (attendanceValue < 1m)
                        {
                            presentCount += 1m - attendanceValue;
                        }

                        continue;
                    }

                    if (IsStatus(attendance?.Status, "H", "Holiday") ||
                        IsOfficeHoliday(date, officeHolidays))
                    {
                        holidayCount += 1m;
                        continue;
                    }

                    if (IsStatus(attendance?.Status, "WO", "Week Off", "WeekOff", "Weekly Off") ||
                        IsWeeklyOff(date, weeklyOffDays))
                    {
                        weekOffCount += 1m;
                        continue;
                    }

                    var dateApprovedLeaves = employeeApprovedLeaves
                        .Where(l =>
                            l.FromDate.Date <= date &&
                            l.ToDate.Date >= date)
                        .ToList();

                    if (dateApprovedLeaves.Any() ||
                        IsStatus(attendance?.Status, "L", "Leave"))
                    {
                        decimal leaveDayValue = dateApprovedLeaves.Any()
                            ? Math.Min(
                                1m,
                                dateApprovedLeaves.Sum(l =>
                                    GetLeaveDayValue(l, date)))
                            : attendanceValue;

                        leaveCount += leaveDayValue;

                        if (dateApprovedLeaves.Any())
                        {
                            decimal paidLeaveValue =
                                dateApprovedLeaves
                                    .Where(l =>
                                        l.LeaveType != null &&
                                        l.LeaveType.IsPaidLeave)
                                    .Sum(l => GetLeaveDayValue(l, date));

                            paidLeaveValue = Math.Min(
                                paidLeaveValue,
                                leaveDayValue);

                            paidLeaveCount += paidLeaveValue;
                            unpaidLeaveCount += leaveDayValue - paidLeaveValue;
                        }
                        else
                        {
                            unpaidLeaveCount += leaveDayValue;
                        }

                        continue;
                    }

                    missingDays += 1m;
                }

                decimal absentCount =
                    markedAbsentCount
                    + missingDays
                    + unpaidLeaveCount;

                decimal payDays =
                    presentCount
                    + onDutyCount
                    + holidayCount
                    + weekOffCount
                    + paidLeaveCount;

                decimal workingDays =
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

        private async Task<HashSet<DateTime>> GetOfficeHolidaysAsync(
            DateTime startDate,
            DateTime endDate)
        {
            var configuredHolidays =
                _configuration
                    .GetSection("Attendance:OfficeHolidays")
                    .Get<string[]>();

            var officeHolidays = new HashSet<DateTime>();

            foreach (var configuredHoliday in configuredHolidays ?? Array.Empty<string>())
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

            var uploadedHolidays = await _context.Holidays
                .AsNoTracking()
                .Where(x =>
                    x.IsActive &&
                    x.HolidayDate.Date >= startDate.Date &&
                    x.HolidayDate.Date <= endDate.Date)
                .Select(x => x.HolidayDate.Date)
                .ToListAsync();

            foreach (var holiday in uploadedHolidays)
            {
                officeHolidays.Add(holiday);
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

        private decimal GetLeaveDayValue(
            AryamanBMS.Models.LeaveApplicationModel leaveApplication,
            DateTime date)
        {
            if (leaveApplication.IsHalfDay &&
                leaveApplication.FromDate.Date == date.Date)
            {
                return 0.5m;
            }

            return 1m;
        }

        private static decimal NormalizeDayValue(decimal value)
        {
            return value == 0.5m
                ? 0.5m
                : 1m;
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
