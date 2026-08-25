using System.Globalization;

using AryamanBMS.Models;
using AryamanBMS.Services.Interface;
using AryamanBMS.ViewModels;
using ClosedXML.Excel;
using AryamanBMS.Repositories.Interfaces;

namespace AryamanBMS.Services
{
    public class HolidayExcelImportService : IHolidayExcelImportService
    {
        private const long MaxUploadBytes = 5 * 1024 * 1024;

        private readonly IHolidayRepository _holidayRepository;

        public HolidayExcelImportService(
    IHolidayRepository holidayRepository)
        {
            _holidayRepository = holidayRepository;
        }

        public async Task<HolidayImportResult> ImportAsync(IFormFile file)
        {
            var result = new HolidayImportResult();

            if (file == null || file.Length == 0)
            {
                result.Errors.Add("Please upload a valid Holiday Excel file.");
                return result;
            }

            if (Path.GetExtension(file.FileName).ToLower() != ".xlsx")
            {
                result.Errors.Add("Only .xlsx Holiday Excel files are allowed.");
                return result;
            }

            if (file.Length > MaxUploadBytes)
            {
                result.Errors.Add("Holiday Excel file size cannot exceed 5 MB.");
                return result;
            }

            XLWorkbook workbook;

            try
            {
                workbook = new XLWorkbook(file.OpenReadStream());
            }
            catch
            {
                result.Errors.Add("Uploaded Excel file could not be read.");
                return result;
            }

            using (workbook)
            {
                var worksheet = workbook.Worksheets.First();

                int headerRow = FindHeaderRow(worksheet);

                if (headerRow == 0)
                {
                    result.Errors.Add("Holiday template header not found. Expected columns: Month, Date, Day, Festival.");
                    return result;
                }

                int startRow = headerRow + 1;
                int lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;

                var seenDates = new HashSet<DateTime>();

                for (int row = startRow; row <= lastRow; row++)
                {
                    string month = worksheet.Cell(row, 1).GetString().Trim();
                    string dateText = worksheet.Cell(row, 2).GetString().Trim();
                    string day = worksheet.Cell(row, 3).GetString().Trim();
                    string festival = worksheet.Cell(row, 4).GetString().Trim();

                    if (festival.Contains("Prepared By", StringComparison.OrdinalIgnoreCase) ||
                         month.Contains("Prepared By", StringComparison.OrdinalIgnoreCase) ||
                         day.Contains("Verified By", StringComparison.OrdinalIgnoreCase) ||
                         festival.Contains("Passed By", StringComparison.OrdinalIgnoreCase))
                    {
                        break;
                    }

                    if (string.IsNullOrWhiteSpace(dateText) &&
                        string.IsNullOrWhiteSpace(festival))
                    {
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(dateText) ||
                        string.IsNullOrWhiteSpace(festival))
                    {
                        result.Errors.Add($"Row {row}: Date and Festival are required.");
                        continue;
                    }

                    if (!DateTime.TryParseExact(
                            dateText,
                            "dd.MM.yyyy",
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.None,
                            out DateTime holidayDate))
                    {
                        result.Errors.Add($"Row {row}: Invalid date '{dateText}'. Use dd.MM.yyyy.");
                        continue;
                    }

                    holidayDate = holidayDate.Date;

                    if (!seenDates.Add(holidayDate))
                    {
                        result.Errors.Add($"Row {row}: Duplicate holiday date '{dateText}' in uploaded file.");
                        continue;
                    }

                    var expectedDay = holidayDate.ToString("dddd", CultureInfo.InvariantCulture);

                    if (!string.IsNullOrWhiteSpace(day) &&
                        !string.Equals(day, expectedDay, StringComparison.OrdinalIgnoreCase))
                    {
                        result.Errors.Add($"Row {row}: Day '{day}' does not match date. Expected '{expectedDay}'.");
                        continue;
                    }

                    var existing = await _holidayRepository.GetByDateAsync(holidayDate);

                    if (existing == null)
                    {
                        await _holidayRepository.AddAsync(new HolidayModel
                        {
                            HolidayDate = holidayDate,
                            HolidayName = festival,
                            MonthName = string.IsNullOrWhiteSpace(month)
                                ? holidayDate.ToString("MMM")
                                : month,
                            DayName = expectedDay,
                            HolidayType = "Office Holiday",
                            IsActive = true,
                            CreatedOn = DateTime.Now
                        });

                        result.AddedCount++;
                    }
                    else
                    {
                        existing.HolidayName = festival;
                        existing.MonthName = string.IsNullOrWhiteSpace(month)
                            ? holidayDate.ToString("MMM")
                            : month;
                        existing.DayName = expectedDay;
                        existing.HolidayType = "Office Holiday";
                        existing.IsActive = true;
                        existing.UpdatedOn = DateTime.Now;

                        result.UpdatedCount++;
                    }
                }

                await _holidayRepository.SaveAsync();
            }

            return result;
        }

        private static int FindHeaderRow(IXLWorksheet worksheet)
        {
            int lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;

            for (int row = 1; row <= lastRow; row++)
            {
                string col1 = worksheet.Cell(row, 1).GetString().Trim();
                string col2 = worksheet.Cell(row, 2).GetString().Trim();
                string col3 = worksheet.Cell(row, 3).GetString().Trim();
                string col4 = worksheet.Cell(row, 4).GetString().Trim();

                if (col1.Equals("Month", StringComparison.OrdinalIgnoreCase) &&
                    col2.Equals("Date", StringComparison.OrdinalIgnoreCase) &&
                    col3.Equals("Day", StringComparison.OrdinalIgnoreCase) &&
                    col4.Equals("Festival", StringComparison.OrdinalIgnoreCase))
                {
                    return row;
                }
            }

            return 0;
        }
    }
}