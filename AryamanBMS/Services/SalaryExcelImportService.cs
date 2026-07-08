using System.Globalization;
using AryamanBMS.Data;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using AryamanBMS.Services.Interface;
using AryamanBMS.ViewModels;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;

namespace AryamanBMS.Services
{
    public class SalaryExcelImportService : ISalaryExcelImportService
    {
        private const long MaxUploadBytes = 5 * 1024 * 1024;

        private static readonly HashSet<string> AllowedExtensions =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ".xlsx"
            };

        private static readonly HashSet<string> AllowedContentTypes =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "application/octet-stream"
            };

        private readonly IEmployeeRepository _employeeRepository;
        private readonly ISalaryRecordRepository _salaryRecordRepository;
        private readonly ApplicationDbContext _context;

        public SalaryExcelImportService(
            IEmployeeRepository employeeRepository,
            ISalaryRecordRepository salaryRecordRepository,
            ApplicationDbContext context)
        {
            _employeeRepository = employeeRepository;
            _salaryRecordRepository = salaryRecordRepository;
            _context = context;
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

            string originalFileName =
                Path.GetFileName(file.FileName);

            string extension =
                Path.GetExtension(originalFileName);

            if (!AllowedExtensions.Contains(extension))
            {
                result.Errors.Add("Only .xlsx salary Excel files are allowed.");
                return result;
            }

            if (file.Length > MaxUploadBytes)
            {
                result.Errors.Add("Salary Excel file size cannot exceed 5 MB.");
                return result;
            }

            if (!string.IsNullOrWhiteSpace(file.ContentType) &&
                !AllowedContentTypes.Contains(file.ContentType))
            {
                result.Errors.Add("Please upload a valid .xlsx salary Excel file.");
                return result;
            }

            using var stream = file.OpenReadStream();

            XLWorkbook workbook;

            try
            {
                workbook = new XLWorkbook(stream);
            }
            catch
            {
                result.Errors.Add("Uploaded Excel file could not be read. Please use the exported salary template.");
                return result;
            }

            using (workbook)
            {
                var worksheet = workbook.Worksheets
                    .FirstOrDefault(x => x.Name.Equals(
                        "Salary",
                        StringComparison.OrdinalIgnoreCase));

                if (worksheet == null)
                {
                    result.Errors.Add("Salary sheet not found in uploaded Excel.");
                    return result;
                }

                var importBatch = new SalaryImportBatchModel
                {
                    SourceFileName = originalFileName,
                    Month = month,
                    Year = year,
                    ImportedOn = DateTime.Now
                };

                await _context.SalaryImportBatches.AddAsync(importBatch);
                await _context.SaveChangesAsync();

                result.ImportBatchId =
                    importBatch.SalaryImportBatchId;

                int startRow = 4;
                int lastRow =
                    worksheet.LastRowUsed()?.RowNumber() ?? 0;

                var seenEmployeeCodes =
                    new HashSet<string>(StringComparer.OrdinalIgnoreCase);

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

                    importBatch.TotalRows++;

                    if (!seenEmployeeCodes.Add(employeeCode))
                    {
                        result.Errors.Add(
                            $"Row {row}: Duplicate employee code '{employeeCode}' in uploaded file.");
                        importBatch.FailedRows++;
                        continue;
                    }

                    var employee = await _employeeRepository.Employees
                        .FirstOrDefaultAsync(x =>
                            x.EmployeeCode == employeeCode);

                    if (employee == null)
                    {
                        result.Errors.Add(
                            $"Row {row}: Employee code '{employeeCode}' not found.");
                        importBatch.FailedRows++;
                        continue;
                    }

                    if (!IsEmployeeEligible(employee, month, year))
                    {
                        result.Errors.Add(
                            $"Row {row} ({employeeCode}): Employee is not eligible for this payroll period.");
                        importBatch.FailedRows++;
                        continue;
                    }

                    var existingSalary =
                        await _salaryRecordRepository.SalaryRecords
                            .FirstOrDefaultAsync(x =>
                                x.EmployeeId == employee.Id &&
                                x.Month == month &&
                                x.Year == year);

                    if (existingSalary != null &&
                        string.Equals(
                            existingSalary.PaymentStatus,
                            "Paid",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        result.Errors.Add(
                            $"Row {row} ({employeeCode}): Salary is already marked as Paid. Reverse/unmark payment before re-importing.");
                        result.SkippedPaidCount++;
                        importBatch.FailedRows++;
                        continue;
                    }

                    if (existingSalary != null &&
                        string.Equals(
                            existingSalary.PayrollStatus,
                            "Finalized",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        result.Errors.Add(
                            $"Row {row} ({employeeCode}): Payroll is finalized. Reopen before importing corrections.");
                        importBatch.FailedRows++;
                        continue;
                    }

                    var missingFormulaColumns =
                        GetMissingSalaryFormulaColumns(
                            worksheet,
                            row);

                    if (missingFormulaColumns.Any())
                    {
                        result.Errors.Add(
                            $"Row {row} ({employeeCode}): Salary formulas are missing in " +
                            $"{string.Join(", ", missingFormulaColumns)}. " +
                            "Please drag/copy the formulas into this row and upload again.");
                        importBatch.FailedRows++;
                        continue;
                    }

                    var salaryRecord =
                        existingSalary ?? new SalaryRecordModel();

                    salaryRecord.EmployeeId = employee.Id;
                    salaryRecord.Month = month;
                    salaryRecord.Year = year;
                    salaryRecord.SalaryImportBatchId =
                        importBatch.SalaryImportBatchId;

                    salaryRecord.ActualSalary = GetDecimal(worksheet, row, 4);
                    salaryRecord.PayDays = GetDecimal(worksheet, row, 5);
                    salaryRecord.StandardMonthlySalary =
                        salaryRecord.ActualSalary;
                    salaryRecord.PayrollDivisor =
                        DateTime.DaysInMonth(year, month);
                    salaryRecord.EligibleEmploymentDays =
                        CountEligibleDays(employee, month, year);
                    salaryRecord.PerDaySalary =
                        salaryRecord.PayrollDivisor > 0
                            ? Math.Round(
                                salaryRecord.ActualSalary /
                                salaryRecord.PayrollDivisor,
                                2)
                            : 0;
                    salaryRecord.ProratedSalary =
                        Math.Round(
                            salaryRecord.PerDaySalary *
                            salaryRecord.PayDays,
                            2);

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
                        salaryRecord.Advance +
                        salaryRecord.TdsDeduction +
                        salaryRecord.OtherDeductions;

                    salaryRecord.PaymentStatus = "Pending";
                    salaryRecord.PayrollStatus = "Generated";
                    salaryRecord.GeneratedOn = DateTime.Now;
                    salaryRecord.SourceFileName = originalFileName;
                    salaryRecord.ImportedOn = DateTime.Now;

                    if (existingSalary == null)
                    {
                        salaryRecord.CreatedOn = DateTime.Now;
                        await _salaryRecordRepository.AddAsync(salaryRecord);
                    }
                    else
                    {
                        salaryRecord.UpdatedOn = DateTime.Now;
                        await _salaryRecordRepository.UpdateAsync(salaryRecord);
                    }

                    result.ImportedCount++;
                    importBatch.SuccessfulRows++;
                }

                importBatch.ErrorSummary =
                    result.Errors.Any()
                        ? string.Join(Environment.NewLine, result.Errors)
                        : null;

                await _salaryRecordRepository.SaveAsync();
                await _context.SaveChangesAsync();
            }

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

        private List<string> GetMissingSalaryFormulaColumns(
            IXLWorksheet worksheet,
            int row)
        {
            var requiredFormulaColumns =
                new Dictionary<int, string>
                {
                    { 6, "Gross Salary" },
                    { 7, "Basic Salary" },
                    { 8, "HRA" },
                    { 9, "Conveyance" },
                    { 10, "Medical Allowance" },
                    { 11, "Education Allowance" },
                    { 12, "Special Allowance" },
                    { 13, "Total Earnings" },
                    { 14, "Gross Minus Conveyance" },
                    { 15, "PF Employee" },
                    { 16, "ESIC Employee" },
                    { 17, "Professional Tax" },
                    { 19, "Total Payable" },
                    { 21, "Employer PF" },
                    { 22, "Employer ESIC" },
                    { 23, "CTC" }
                };

            var missingColumns = new List<string>();

            foreach (var item in requiredFormulaColumns)
            {
                var cell = worksheet.Cell(row, item.Key);

                if (string.IsNullOrWhiteSpace(cell.FormulaA1))
                {
                    missingColumns.Add(item.Value);
                }
            }

            return missingColumns;
        }

        private static bool IsEmployeeEligible(
            EmployeeModel employee,
            int month,
            int year)
        {
            DateTime start = new(year, month, 1);
            DateTime end = new(year, month, DateTime.DaysInMonth(year, month));

            return employee.JoiningDate.Date <= end &&
                   (!employee.LastWorkingDate.HasValue ||
                    employee.LastWorkingDate.Value.Date >= start);
        }

        private static int CountEligibleDays(
            EmployeeModel employee,
            int month,
            int year)
        {
            DateTime start = new(year, month, 1);
            DateTime end = new(year, month, DateTime.DaysInMonth(year, month));

            DateTime eligibleStart =
                employee.JoiningDate.Date > start
                    ? employee.JoiningDate.Date
                    : start;

            DateTime eligibleEnd =
                employee.LastWorkingDate.HasValue &&
                employee.LastWorkingDate.Value.Date < end
                    ? employee.LastWorkingDate.Value.Date
                    : end;

            if (eligibleStart > eligibleEnd)
            {
                return 0;
            }

            return (eligibleEnd - eligibleStart).Days + 1;
        }
    }
}
