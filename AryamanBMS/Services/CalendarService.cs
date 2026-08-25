using AryamanBMS.Business.Interfaces;
using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using AryamanBMS.Services.Interfaces;
using AryamanBMS.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AryamanBMS.Services
{
    public class CalendarService : ICalendarService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUserModel> _userManager;

        private readonly IWorkingDayService _workingDayService;
        private readonly ICalendarManualEventRepository _calendarManualEventRepository;

        public CalendarService(
    ApplicationDbContext context,
    UserManager<ApplicationUserModel> userManager,
    IWorkingDayService workingDayService,
    ICalendarManualEventRepository calendarManualEventRepository)
        {
            _context = context;
            _userManager = userManager;
            _workingDayService = workingDayService;
            _calendarManualEventRepository =
                calendarManualEventRepository;
        }

        public async Task<List<CalendarEventViewModel>> GetEventsAsync(
             ClaimsPrincipal user,
             DateTime start,
             DateTime end,
             bool personalOnly = false)
        {
            var events = new List<CalendarEventViewModel>();

            bool isAdmin = user.IsInRole("Admin");
            bool isHR = user.IsInRole("HR");
            bool isMaster = user.IsInRole("Master");

            bool canViewAll =
                !personalOnly &&
                (isAdmin || isHR || isMaster);

            if (canViewAll)
            {
                await AddNonWorkingDayBackgroundsAsync(events, start, end);
                await AddBirthdaysAsync(events, start, end);
                await AddHolidaysAsync(events, start, end);
                await AddAllLeavesAsync(events, start, end);
                await AddAttendanceExceptionsAsync(events, start, end);
                await AddAllTasksAsync(events, start, end);
                await AddAllMeetingsAsync(events, start, end);

                if (isAdmin || isMaster)
                {
                    await AddBillingMilestonesAsync(events, start, end);
                }

                await AddManualEventsAsync(events, start, end);

                return events
                    .OrderBy(x => x.Start)
                    .ThenBy(x => x.Type)
                    .ToList();
            }

            var appUser = await _userManager.GetUserAsync(user);

            if (appUser == null)
            {
                return events;
            }

            var employeeId = await _context.Employees
                .Where(x => x.ApplicationUserId == appUser.Id)
                .Select(x => (int?)x.Id)
                .FirstOrDefaultAsync();

            if (!employeeId.HasValue)
            {
                return events;
            }

            await AddNonWorkingDayBackgroundsAsync(events, start, end);
            await AddBirthdaysAsync(events, start, end);
            await AddHolidaysAsync(events, start, end);
            await AddEmployeeLeavesAsync(events, employeeId.Value, start, end);
            await AddEmployeeTasksAsync(events, employeeId.Value, start, end);
            await AddEmployeeMeetingsAsync(events, employeeId.Value, start, end);
            await AddManualEventsAsync(
                events,
                start,
                end,
                appUser.Id);

            return events
                .OrderBy(x => x.Start)
                .ThenBy(x => x.Type)
                .ToList();
        }

        private async Task AddEmployeeLeavesAsync(
            List<CalendarEventViewModel> events,
            int employeeId,
            DateTime start,
            DateTime end)
        {
            var leaves = await _context.LeaveApplications
                .AsNoTracking()
                .Include(x => x.LeaveType)
                .Include(x => x.LeaveDays)
                .Where(x =>
                    x.EmployeeId == employeeId &&
                    x.FromDate.Date <= end.Date &&
                    x.ToDate.Date >= start.Date)
                .ToListAsync();

            foreach (var leave in leaves)
            {
                await AddWorkingDayLeaveEventsAsync(
                    events,
                    leave,
                    leave.LeaveType?.LeaveName ?? "Leave",
                    start,
                    end);
            }
        }

        private async Task AddWorkingDayLeaveEventsAsync(
            List<CalendarEventViewModel> events,
            LeaveApplicationModel leave,
            string title,
            DateTime start,
            DateTime end)
        {
            var rangeStart = leave.FromDate.Date > start.Date
                ? leave.FromDate.Date
                : start.Date;

            var rangeEnd = leave.ToDate.Date < end.Date
                ? leave.ToDate.Date
                : end.Date;

            var leaveDates = GetCalendarLeaveDates(leave, rangeStart, rangeEnd);

            foreach (var date in leaveDates)
            {
                if (!await _workingDayService.IsWorkingDayAsync(date))
                {
                    continue;
                }

                events.Add(new CalendarEventViewModel
                {
                    Title = title,
                    Start = date,
                    End = date.AddDays(1),
                    AllDay = true,
                    Type = "Leave",
                    Status = leave.IsHalfDay
                        ? $"{leave.Status} Half Day"
                        : leave.Status,
                    Color = "#16a34a",
                    TextColor = "#ffffff",
                    Url = $"/LeaveApplication/Details/{leave.Id}"
                });
            }
        }

        private static List<DateTime> GetCalendarLeaveDates(
            LeaveApplicationModel leave,
            DateTime rangeStart,
            DateTime rangeEnd)
        {
            if (leave.LeaveDays.Any())
            {
                return leave.LeaveDays
                    .Where(x =>
                        !string.Equals(
                            x.Status,
                            "Cancelled",
                            StringComparison.OrdinalIgnoreCase) &&
                        x.LeaveDate.Date >= rangeStart.Date &&
                        x.LeaveDate.Date <= rangeEnd.Date)
                    .Select(x => x.LeaveDate.Date)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();
            }

            var dates = new List<DateTime>();

            for (var date = rangeStart.Date;
                 date <= rangeEnd.Date;
                 date = date.AddDays(1))
            {
                dates.Add(date);
            }

            return dates;
        }

        private async Task AddEmployeeTasksAsync(
            List<CalendarEventViewModel> events,
            int employeeId,
            DateTime start,
            DateTime end)
        {
            var tasks = await _context.ProjectTasks
                .AsNoTracking()
                .Include(x => x.Project)
                .Where(x =>
                    x.IsActive &&
                    x.AssignedEmployeeId == employeeId &&
                    x.DueDate.HasValue &&
                    x.DueDate.Value.Date >= start.Date &&
                    x.DueDate.Value.Date <= end.Date)
                .ToListAsync();

            foreach (var task in tasks)
            {
                events.Add(new CalendarEventViewModel
                {
                    Title = task.TaskTitle,
                    Start = task.DueDate!.Value.Date,
                    Type = "Task",
                    Status = task.Status,
                    Color = "#dc2626",
                    TextColor = "#ffffff",
                    Url = $"/ProjectTask/Details/{task.Id}"
                });
            }
        }

        private async Task AddEmployeeMeetingsAsync(
            List<CalendarEventViewModel> events,
            int employeeId,
            DateTime start,
            DateTime end)
        {
            var meetings = await _context.ProjectMeetingAttendees
                .AsNoTracking()
                .Include(x => x.Meeting)
                    .ThenInclude(x => x!.Project)
                .Where(x =>
                    x.EmployeeId == employeeId &&
                    x.Meeting != null &&
                    x.Meeting.IsActive &&
                    x.Meeting.MeetingDate.Date >= start.Date &&
                    x.Meeting.MeetingDate.Date <= end.Date)
                .ToListAsync();

            foreach (var attendee in meetings)
            {
                var meeting = attendee.Meeting!;

                events.Add(new CalendarEventViewModel
                {
                    Title = meeting.MeetingTitle,
                    Start = meeting.MeetingDate.Date.Add(meeting.StartTime),
                    End = meeting.EndTime.HasValue
                        ? meeting.MeetingDate.Date.Add(meeting.EndTime.Value)
                        : null,
                    Type = "Meeting",
                    Status = meeting.MeetingStatus,
                    Color = "#f97316",
                    TextColor = "#ffffff",
                    Url = $"/MOM/Details/{meeting.Id}"
                });
            }
        }

        private async Task AddAllLeavesAsync(
            List<CalendarEventViewModel> events,
            DateTime start,
            DateTime end)
        {
            var leaves = await _context.LeaveApplications
                .AsNoTracking()
                .Include(x => x.Employee)
                .Include(x => x.LeaveType)
                .Include(x => x.LeaveDays)
                .Where(x =>
                    x.FromDate.Date <= end.Date &&
                    x.ToDate.Date >= start.Date)
                .ToListAsync();

            foreach (var leave in leaves)
            {
                string employeeName =
                    leave.Employee?.FullName ?? $"Employee #{leave.EmployeeId}";

                string leaveName =
                    leave.LeaveType?.LeaveName ?? "Leave";

                await AddWorkingDayLeaveEventsAsync(
                    events,
                    leave,
                    $"{employeeName} - {leaveName}",
                    start,
                    end);
            }
        }

        private async Task AddAttendanceExceptionsAsync(
            List<CalendarEventViewModel> events,
            DateTime start,
            DateTime end)
        {
            var records = await _context.Attendances
                .AsNoTracking()
                .Include(x => x.Employee)
                .Where(x =>
                    x.AttendanceDate.Date >= start.Date &&
                    x.AttendanceDate.Date <= end.Date &&
                    (
                        x.Status == "A" ||
                        x.Status == "HD" ||
                        x.AttendanceValue == 0.5m ||
                        !x.CheckInTime.HasValue ||
                        !x.CheckOutTime.HasValue
                    ))
                .ToListAsync();

            foreach (var record in records)
            {
                string employeeName =
                    record.Employee?.FullName ?? $"Employee #{record.EmployeeId}";

                string status =
                    record.Status == "A" ? "Absent" :
                    record.Status == "HD" || record.AttendanceValue == 0.5m ? "Half Day" :
                    !record.CheckInTime.HasValue ? "Missing Check-In" :
                    !record.CheckOutTime.HasValue ? "Missing Check-Out" :
                    record.Status;

                events.Add(new CalendarEventViewModel
                {
                    Title = $"{employeeName} - {status}",
                    Start = record.AttendanceDate.Date,
                    Type = "Attendance",
                    Status = status,
                    Color = "#1d4ed8",
                    TextColor = "#ffffff",
                    Url = "/Attendance/Register"
                });
            }
        }

        private async Task AddAllTasksAsync(
            List<CalendarEventViewModel> events,
            DateTime start,
            DateTime end)
        {
            var tasks = await _context.ProjectTasks
                .AsNoTracking()
                .Include(x => x.Project)
                .Include(x => x.AssignedEmployee)
                .Where(x =>
                    x.IsActive &&
                    x.DueDate.HasValue &&
                    x.DueDate.Value.Date >= start.Date &&
                    x.DueDate.Value.Date <= end.Date)
                .ToListAsync();

            foreach (var task in tasks)
            {
                string assignee =
                    task.AssignedEmployee?.FullName ?? "Unassigned";

                events.Add(new CalendarEventViewModel
                {
                    Title = $"{task.TaskTitle} - {assignee}",
                    Start = task.DueDate!.Value.Date,
                    Type = "Task",
                    Status = task.Status,
                    Color = "#dc2626",
                    TextColor = "#ffffff",
                    Url = $"/ProjectTask/Details/{task.Id}"
                });
            }
        }

        private async Task AddAllMeetingsAsync(
            List<CalendarEventViewModel> events,
            DateTime start,
            DateTime end)
        {
            var meetings = await _context.ProjectMeetings
                .AsNoTracking()
                .Include(x => x.Project)
                .Where(x =>
                    x.IsActive &&
                    x.MeetingDate.Date >= start.Date &&
                    x.MeetingDate.Date <= end.Date)
                .ToListAsync();

            foreach (var meeting in meetings)
            {
                events.Add(new CalendarEventViewModel
                {
                    Title = $"{meeting.MeetingTitle} - {meeting.Project?.ProjectName ?? "Project"}",
                    Start = meeting.MeetingDate.Date.Add(meeting.StartTime),
                    End = meeting.EndTime.HasValue
                        ? meeting.MeetingDate.Date.Add(meeting.EndTime.Value)
                        : null,
                    Type = "Meeting",
                    Status = meeting.MeetingStatus,
                    Color = "#f97316",
                    TextColor = "#ffffff",
                    Url = $"/MOM/Details/{meeting.Id}"
                });
            }
        }

        private async Task AddBillingMilestonesAsync(
            List<CalendarEventViewModel> events,
            DateTime start,
            DateTime end)
        {
            var milestones = await _context.BillingMilestones
                .AsNoTracking()
                .Include(x => x.Project)
                .Where(x =>
                    x.IsActive &&
                    x.ApprovalDate.HasValue &&
                    x.ApprovalDate.Value.Date >= start.Date &&
                    x.ApprovalDate.Value.Date <= end.Date &&
                    x.CompletionStatus != "Completed")
                .ToListAsync();

            foreach (var milestone in milestones)
            {
                events.Add(new CalendarEventViewModel
                {
                    Title = $"Billing - {milestone.MilestoneName}",
                    Start = milestone.ApprovalDate!.Value.Date,
                    Type = "Billing",
                    Status = milestone.CompletionStatus,
                    Color = "#fef3c7",
                    TextColor = "#92400e",
                    Url = $"/BillingMilestone/Details/{milestone.BillingMilestoneId}"
                });
            }
        }

        private async Task AddManualEventsAsync(
            List<CalendarEventViewModel> events,
            DateTime start,
            DateTime end,
            string? personalUserId = null)
        {
            var manualEvents = await _calendarManualEventRepository
    .GetActiveEventsAsync(
        start,
        end,
        personalUserId);

            foreach (var item in manualEvents)
            {
                events.Add(new CalendarEventViewModel
                {
                    Id = item.Id,
                    Title = item.Title,
                    Start = item.StartDateTime,
                    End = item.EndDateTime,
                    AllDay = item.IsAllDay,
                    Type = item.EventType,
                    Status = "Manual",
                    IsManual = true,
                    Color = "#64748b",
                    TextColor = "#ffffff",
                    Url = null
                });
            }
        }

        private async Task AddHolidaysAsync(
            List<CalendarEventViewModel> events,
            DateTime start,
            DateTime end)
        {
            var holidays = await _context.Holidays
                .AsNoTracking()
                .Where(x =>
                    x.IsActive &&
                    x.HolidayDate.Date >= start.Date &&
                    x.HolidayDate.Date <= end.Date)
                .OrderBy(x => x.HolidayDate)
                .ToListAsync();

            foreach (var holiday in holidays)
            {
                events.Add(new CalendarEventViewModel
                {
                    Title = holiday.HolidayName,
                    Start = holiday.HolidayDate.Date,
                    End = holiday.HolidayDate.Date.AddDays(1),
                    AllDay = true,
                    Type = "Holiday",
                    Status = holiday.HolidayType,
                    Color = "#7c3aed",
                    TextColor = "#ffffff",
                    Url = null
                });
            }
        }

        private async Task AddNonWorkingDayBackgroundsAsync(
    List<CalendarEventViewModel> events,
    DateTime start,
    DateTime end)
        {
            for (var date = start.Date;
                 date < end.Date;
                 date = date.AddDays(1))
            {
                var status =
                    await _workingDayService.GetDayStatusAsync(date);

                if (status == "Holiday")
                {
                    events.Add(new CalendarEventViewModel
                    {
                        Title = "Company Holiday",
                        Start = date,
                        End = date.AddDays(1),
                        AllDay = true,
                        Display = "background",
                        Type = "Holiday",
                        Status = "Company Holiday",
                        Color = "#ede9fe",
                        TextColor = "#5b21b6"
                    });
                }
                else if (status == "WeeklyOff")
                {
                    events.Add(new CalendarEventViewModel
                    {
                        Title = "Weekly Off",
                        Start = date,
                        End = date.AddDays(1),
                        AllDay = true,
                        Display = "background",
                        Type = "WeeklyOff",
                        Status = "Weekly Off",
                        Color = "#e2e8f0",
                        TextColor = "#334155"
                    });
                }
            }
        }

        private async Task AddBirthdaysAsync(
    List<CalendarEventViewModel> events,
    DateTime start,
    DateTime end)
        {
            var employees =
                await _context.Employees
                    .AsNoTracking()
                    .Where(x =>
                        x.IsActive &&
                        x.DateOfBirth.HasValue)
                    .Select(x => new
                    {
                        x.Id,
                        x.FullName,
                        DateOfBirth = x.DateOfBirth!.Value
                    })
                    .ToListAsync();

            foreach (var employee in employees)
            {
                for (int year = start.Year - 1;
                     year <= end.Year + 1;
                     year++)
                {
                    DateTime birthday;

                    if (employee.DateOfBirth.Month == 2 &&
                        employee.DateOfBirth.Day == 29 &&
                        !DateTime.IsLeapYear(year))
                    {
                        birthday = new DateTime(year, 2, 28);
                    }
                    else
                    {
                        birthday = new DateTime(
                            year,
                            employee.DateOfBirth.Month,
                            employee.DateOfBirth.Day);
                    }

                    if (birthday.Date < start.Date ||
                        birthday.Date > end.Date)
                    {
                        continue;
                    }

                    events.Add(new CalendarEventViewModel
                    {
                        Title = $"{employee.FullName} - Birthday",
                        Start = birthday.Date,
                        AllDay = true,
                        Type = "Birthday",
                        Status = "Birthday",
                        Color = "#db2777",
                        TextColor = "#ffffff",
                        Url = null
                    });
                }
            }
        }
    }
}