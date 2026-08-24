using AryamanBMS.Business.Interfaces;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using AryamanBMS.Services.Interface;
using AryamanBMS.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Services
{
    public class SalaryAttendanceSummaryService
        : ISalaryAttendanceSummaryService
    {
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IAttendanceRepository _attendanceRepository;
        private readonly ILeaveApplicationRepository _leaveApplicationRepository;
        private readonly IAttendanceSummaryCalculator _attendanceSummaryCalculator;

        public SalaryAttendanceSummaryService(
            IEmployeeRepository employeeRepository,
            IAttendanceRepository attendanceRepository,
            ILeaveApplicationRepository leaveApplicationRepository,
            IAttendanceSummaryCalculator attendanceSummaryCalculator)
        {
            _employeeRepository = employeeRepository;
            _attendanceRepository = attendanceRepository;
            _leaveApplicationRepository = leaveApplicationRepository;
            _attendanceSummaryCalculator = attendanceSummaryCalculator;
        }

        public async Task<List<AttendanceSummaryViewModel>>
            GetMonthlySummaryAsync(int month, int year)
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

            var approvedLeaves = await _leaveApplicationRepository
                .LeaveApplications
                .Where(l =>
                    l.Status == "Approved" &&
                    l.FromDate <= endDate &&
                    l.ToDate >= startDate)
                .ToListAsync();

            var input = new AttendanceSummaryCalculationInput
            {
                Month = month,
                Year = year,
                Employees = employees
                    .Select(employee =>
                        new AttendanceSummaryEmployeeInput
                        {
                            EmployeeId = employee.Id,
                            EmployeeCode =
                                employee.EmployeeCode ?? string.Empty,
                            EmployeeName = employee.FullName,
                            JoiningDate = employee.JoiningDate,
                            LastWorkingDate = employee.LastWorkingDate,
                            IsActive = employee.IsActive,

                            Attendances = attendanceRecords
                                .Where(a =>
                                    a.EmployeeId == employee.Id)
                                .Select(a =>
                                    new AttendanceSummaryAttendanceInput
                                    {
                                        AttendanceRecordId = a.Id,
                                        AttendanceDate =
                                            a.AttendanceDate,
                                        Status = a.Status,
                                        AttendanceValue =
                                            a.AttendanceValue
                                    })
                                .ToList(),

                            ApprovedLeaves = approvedLeaves
                                .Where(l =>
                                    l.EmployeeId == employee.Id)
                                .Select(l =>
                                    new AttendanceSummaryLeaveInput
                                    {
                                        FromDate = l.FromDate,
                                        ToDate = l.ToDate,
                                        NumberOfDays =
                                            l.NumberOfDays,
                                        PaidDays = l.PaidDays,
                                        IsHalfDay = l.IsHalfDay
                                    })
                                .ToList()
                        })
                    .ToList()
            };

            var results =
                await _attendanceSummaryCalculator.CalculateAsync(input);

            return results
                .Select(result => new AttendanceSummaryViewModel
                {
                    EmployeeId = result.EmployeeId,
                    EmployeeCode = result.EmployeeCode,
                    EmployeeName = result.EmployeeName,
                    Month = result.Month,
                    Year = result.Year,
                    PresentCount = result.PresentCount,
                    AbsentCount = result.AbsentCount,
                    LeaveCount = result.LeaveCount,
                    PaidLeaveCount = result.PaidLeaveCount,
                    UnpaidLeaveCount = result.UnpaidLeaveCount,
                    HolidayCount = result.HolidayCount,
                    WeekOffCount = result.WeekOffCount,
                    OnDutyCount = result.OnDutyCount,
                    TotalDays = result.TotalDays,
                    PayDays = result.PayDays,
                    MarkedAbsentCount = result.MarkedAbsentCount,
                    MissingDays = result.MissingDays,
                    AttendancePercentage =
                        result.AttendancePercentage
                })
                .ToList();
        }
    }
}