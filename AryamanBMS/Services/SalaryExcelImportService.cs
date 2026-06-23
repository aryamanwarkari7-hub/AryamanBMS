using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using AryamanBMS.Services.Interface;
using AryamanBMS.ViewModels;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace AryamanBMS.Services
{
    public class SalaryExcelImportService : ISalaryExcelImportService
    {
        private readonly IEmployeeRepository _employeeRepository;

        private readonly ISalaryRecordRepository _salaryRecordRepository;

        public SalaryExcelImportService(
            IEmployeeRepository employeeRepository,
            ISalaryRecordRepository salaryRecordRepository)
        {
            _employeeRepository = employeeRepository;
            _salaryRecordRepository = salaryRecordRepository;
        }

        public async Task<SalaryImportResult> ImportAsync(
            IFormFile file,
            int month,
            int year)
        {
            var result = new SalaryImportResult();

            if (file == null || file.Length == 0)
            {
                result.Errors.Add("Please upload a valid Excel file.");
                return result;
            }

            using var stream = file.OpenReadStream();

            using var workbook = new XLWorkbook(stream);

            var worksheet = workbook.Worksheets
                .FirstOrDefault(x => x.Name.Equals(
                    "Salary",
                    StringComparison.OrdinalIgnoreCase));

            if (worksheet == null)
            {
                result.Errors.Add("Salary sheet not found in uploaded Excel.");
                return result;
            }

            int startRow = 4;

            int lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;

            for (int row = startRow; row <= lastRow; row++)
            {
                string employeeCode = worksheet.Cell(row, 2)
                    .GetString()
                    .Trim();

                string employeeName = worksheet.Cell(row, 3)
                    .GetString()
                    .Trim();

                if (string.IsNullOrWhiteSpace(employeeCode))
                {
                    continue;
                }

                if (employeeName.Contains(
                    "Grand Total",
                    StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var employee = await _employeeRepository.Employees
                    .FirstOrDefaultAsync(x => x.EmployeeCode == employeeCode);

                if (employee == null)
                {
                    result.Errors.Add(
                        $"Row {row}: Employee code '{employeeCode}' not found.");

                    continue;
                }

                var existingSalary = await _salaryRecordRepository.SalaryRecords
                    .FirstOrDefaultAsync(x =>
                        x.EmployeeId == employee.Id &&
                        x.Month == month &&
                        x.Year == year);

                if (existingSalary != null &&
                    existingSalary.PaymentStatus == "Paid")
                {
                    result.Errors.Add(
                        $"Row {row}: Salary already paid for employee '{employeeCode}'. Cannot overwrite.");

                    continue;
                }

                var salaryRecord = existingSalary ?? new SalaryRecordModel();

                salaryRecord.EmployeeId = employee.Id;
                salaryRecord.Month = month;
                salaryRecord.Year = year;

                salaryRecord.ActualSalary = GetDecimal(worksheet, row, 4);
                salaryRecord.PayDays = GetDecimal(worksheet, row, 5);

                salaryRecord.GrossSalary = GetDecimal(worksheet, row, 6);

                salaryRecord.BasicSalary = GetDecimal(worksheet, row, 7);
                salaryRecord.HRA = GetDecimal(worksheet, row, 8);
                salaryRecord.Conveyance = GetDecimal(worksheet, row, 9);
                salaryRecord.MedicalAllowance = GetDecimal(worksheet, row, 10);
                salaryRecord.EducationAllowance = GetDecimal(worksheet, row, 11);
                salaryRecord.SpecialAllowance = GetDecimal(worksheet, row, 12);

                salaryRecord.TotalEarnings = GetDecimal(worksheet, row, 13);
                salaryRecord.GrossMinusConveyance = GetDecimal(worksheet, row, 14);

                salaryRecord.PfDeduction = GetDecimal(worksheet, row, 15);
                salaryRecord.EsicDeduction = GetDecimal(worksheet, row, 16);
                salaryRecord.ProfessionalTax = GetDecimal(worksheet, row, 17);
                salaryRecord.Advance = GetDecimal(worksheet, row, 18);

                salaryRecord.NetSalary = GetDecimal(worksheet, row, 19);

                salaryRecord.Remark = worksheet.Cell(row, 20)
                    .GetString()
                    .Trim();

                salaryRecord.EmployerPf = GetDecimal(worksheet, row, 21);
                salaryRecord.EmployerEsic = GetDecimal(worksheet, row, 22);
                salaryRecord.CTC = GetDecimal(worksheet, row, 23);

                salaryRecord.TotalDeductions =
                    salaryRecord.PfDeduction +
                    salaryRecord.EsicDeduction +
                    salaryRecord.ProfessionalTax +
                    salaryRecord.Advance;

                salaryRecord.PaymentStatus = "Pending";
                salaryRecord.SourceFileName = file.FileName;
                salaryRecord.ImportedOn = DateTime.Now;

                if (existingSalary == null)
                {
                    await _salaryRecordRepository.AddAsync(salaryRecord);
                }
                else
                {
                    await _salaryRecordRepository.UpdateAsync(salaryRecord);
                }

                result.ImportedCount++;
            }

            await _salaryRecordRepository.SaveAsync();

            return result;
        }

        private decimal GetDecimal(
            IXLWorksheet worksheet,
            int row,
            int column)
        {
            var cell = worksheet.Cell(row, column);

            if (cell.TryGetValue<decimal>(out var value))
            {
                return value;
            }

            var text = cell.GetFormattedString()
                .Replace(",", "")
                .Trim();

            if (decimal.TryParse(
                text,
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out var parsedValue))
            {
                return parsedValue;
            }

            return 0;
        }
    }
}