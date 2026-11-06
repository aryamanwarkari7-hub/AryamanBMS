using System.Globalization;
using AryamanBMS.Models;
using AryamanBMS.Repositories.Interfaces;
using AryamanBMS.Services.Interface;
using AryamanBMS.ViewModels;
using ClosedXML.Excel;

namespace AryamanBMS.Services
{
    public class HolidayExcelImportService : IHolidayExcelImportService
    {
        private const long MaxUploadBytes = 5 * 1024 * 1024;

        private readonly IHolidayRepository _holidayRepository;

        public HolidayExcelImportService(IHolidayRepository holidayRepository)
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

                var headerMap = FindHeaderMapping(worksheet);

                if (headerMap == null || headerMap.DateCol == 0 || headerMap.NameCol == 0)
                {
                    result.Errors.Add(
                        "Holiday header not found. Excel must contain columns for 'Date' and 'Holiday Name' (or 'Festival')."
                    );
                    return result;
                }

                int startRow = headerMap.RowNumber + 1;
                int lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;

                var seenDates = new HashSet<DateTime>();

                for (int row = startRow; row <= lastRow; row++)
                {
                    var dateCell = worksheet.Cell(row, headerMap.DateCol);
                    var nameCell = worksheet.Cell(row, headerMap.NameCol);

                    string holidayName = nameCell.GetString().Trim();

                    // Detect signature/summary footer rows
                    if (
                        holidayName.Contains("Prepared By", StringComparison.OrdinalIgnoreCase)
                        || holidayName.Contains("Verified By", StringComparison.OrdinalIgnoreCase)
                        || holidayName.Contains("Passed By", StringComparison.OrdinalIgnoreCase)
                    )
                    {
                        break;
                    }

                    if (dateCell.IsEmpty() && string.IsNullOrWhiteSpace(holidayName))
                    {
                        continue;
                    }

                    if (dateCell.IsEmpty() || string.IsNullOrWhiteSpace(holidayName))
                    {
                        result.Errors.Add($"Row {row}: Date and Holiday Name are required.");
                        continue;
                    }

                    // Flexible date parsing: supports Excel Date types, yyyy-MM-dd, dd-MM-yyyy, dd.MM.yyyy, etc.
                    DateTime holidayDate;
                    if (dateCell.DataType == XLDataType.DateTime)
                    {
                        holidayDate = dateCell.GetDateTime().Date;
                    }
                    else if (dateCell.TryGetValue(out DateTime parsedDt))
                    {
                        holidayDate = parsedDt.Date;
                    }
                    else
                    {
                        string dateText = dateCell.GetString().Trim();
                        string[] dateFormats = new[]
                        {
                            "yyyy-MM-dd",
                            "dd.MM.yyyy",
                            "dd-MM-yyyy",
                            "dd/MM/yyyy",
                            "d-MMM-yyyy",
                            "dd-MMM-yyyy",
                            "d-MMMM-yyyy",
                            "dd-MMMM-yyyy",
                            "MM/dd/yyyy",
                        };

                        if (
                            !DateTime.TryParseExact(
                                dateText,
                                dateFormats,
                                CultureInfo.InvariantCulture,
                                DateTimeStyles.None,
                                out holidayDate
                            )
                            && !DateTime.TryParse(
                                dateText,
                                CultureInfo.CurrentCulture,
                                DateTimeStyles.None,
                                out holidayDate
                            )
                        )
                        {
                            result.Errors.Add(
                                $"Row {row}: Invalid date '{dateText}'. Use yyyy-MM-dd, dd-MM-yyyy, or Excel date."
                            );
                            continue;
                        }
                    }

                    holidayDate = holidayDate.Date;

                    if (!seenDates.Add(holidayDate))
                    {
                        result.Errors.Add(
                            $"Row {row}: Duplicate holiday date '{holidayDate:yyyy-MM-dd}' in uploaded file."
                        );
                        continue;
                    }

                    string month =
                        headerMap.MonthCol > 0
                            ? worksheet.Cell(row, headerMap.MonthCol).GetString().Trim()
                            : string.Empty;
                    string day =
                        headerMap.DayCol > 0
                            ? worksheet.Cell(row, headerMap.DayCol).GetString().Trim()
                            : string.Empty;
                    string holidayType =
                        headerMap.TypeCol > 0
                            ? worksheet.Cell(row, headerMap.TypeCol).GetString().Trim()
                            : "Office Holiday";
                    string status =
                        headerMap.StatusCol > 0
                            ? worksheet.Cell(row, headerMap.StatusCol).GetString().Trim()
                            : "Active";

                    if (string.IsNullOrWhiteSpace(holidayType))
                    {
                        holidayType = "Office Holiday";
                    }

                    bool isActive = !status.Equals("Inactive", StringComparison.OrdinalIgnoreCase);

                    var expectedDay = holidayDate.ToString("dddd", CultureInfo.InvariantCulture);

                    var existing = await _holidayRepository.GetByDateAsync(holidayDate);

                    if (existing == null)
                    {
                        await _holidayRepository.AddAsync(
                            new HolidayModel
                            {
                                HolidayDate = holidayDate,
                                HolidayName = holidayName,
                                MonthName = string.IsNullOrWhiteSpace(month)
                                    ? holidayDate.ToString("MMM")
                                    : month,
                                DayName = expectedDay,
                                HolidayType = holidayType,
                                IsActive = isActive,
                                IsConfirmed = holidayDate.Date < DateTime.Today,
                                CreatedOn = DateTime.Now,
                            }
                        );

                        result.AddedCount++;
                    }
                    else
                    {
                        existing.HolidayName = holidayName;
                        existing.MonthName = string.IsNullOrWhiteSpace(month)
                            ? holidayDate.ToString("MMM")
                            : month;
                        existing.DayName = expectedDay;
                        existing.HolidayType = holidayType;
                        existing.IsActive = isActive;
                        existing.IsConfirmed = holidayDate.Date < DateTime.Today;
                        existing.UpdatedOn = DateTime.Now;

                        result.UpdatedCount++;
                    }
                }

                await _holidayRepository.SaveAsync();
            }

            return result;
        }

        private class HolidayHeaderMapping
        {
            public int RowNumber { get; set; }
            public int DateCol { get; set; }
            public int NameCol { get; set; }
            public int MonthCol { get; set; }
            public int DayCol { get; set; }
            public int TypeCol { get; set; }
            public int StatusCol { get; set; }
        }

        private static HolidayHeaderMapping? FindHeaderMapping(IXLWorksheet worksheet)
        {
            int lastRow = Math.Min(worksheet.LastRowUsed()?.RowNumber() ?? 0, 15);

            for (int row = 1; row <= lastRow; row++)
            {
                var mapping = new HolidayHeaderMapping { RowNumber = row };
                int lastCol = worksheet.Row(row).LastCellUsed()?.Address.ColumnNumber ?? 0;

                for (int col = 1; col <= lastCol; col++)
                {
                    string header = worksheet.Cell(row, col).GetString().Trim();

                    if (
                        header.Equals("Date", StringComparison.OrdinalIgnoreCase)
                        || header.Equals("Holiday Date", StringComparison.OrdinalIgnoreCase)
                    )
                    {
                        mapping.DateCol = col;
                    }
                    else if (
                        header.Equals("Holiday Name", StringComparison.OrdinalIgnoreCase)
                        || header.Equals("Holiday", StringComparison.OrdinalIgnoreCase)
                        || header.Equals("Festival", StringComparison.OrdinalIgnoreCase)
                        || header.Equals("Occasion", StringComparison.OrdinalIgnoreCase)
                    )
                    {
                        mapping.NameCol = col;
                    }
                    else if (header.Equals("Month", StringComparison.OrdinalIgnoreCase))
                    {
                        mapping.MonthCol = col;
                    }
                    else if (header.Equals("Day", StringComparison.OrdinalIgnoreCase))
                    {
                        mapping.DayCol = col;
                    }
                    else if (
                        header.Equals("Type", StringComparison.OrdinalIgnoreCase)
                        || header.Equals("Holiday Type", StringComparison.OrdinalIgnoreCase)
                    )
                    {
                        mapping.TypeCol = col;
                    }
                    else if (header.Equals("Status", StringComparison.OrdinalIgnoreCase))
                    {
                        mapping.StatusCol = col;
                    }
                }

                if (mapping.DateCol > 0 && mapping.NameCol > 0)
                {
                    return mapping;
                }
            }

            return null;
        }
    }
}
