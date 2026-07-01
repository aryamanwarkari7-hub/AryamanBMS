using AryamanBMS.Services.Interface;

namespace AryamanBMS.Services
{
        public class FinancialYearService : IFinancialYearService
        {
            public string GetFinancialYear(DateTime date)
            {
                int startYear;

                if (date.Month >= 4)
                {
                    startYear = date.Year;
                }
                else
                {
                    startYear = date.Year - 1;
                }

                int endYear = startYear + 1;

                return $"{startYear}-{endYear.ToString().Substring(2)}";
            }

            public DateTime GetFinancialYearStartDate(DateTime date)
            {
                int year = date.Month >= 4
                    ? date.Year
                    : date.Year - 1;

                return new DateTime(year, 4, 1);
            }

            public DateTime GetFinancialYearEndDate(DateTime date)
            {
                int year = date.Month >= 4
                    ? date.Year + 1
                    : date.Year;

                return new DateTime(year, 3, 31, 23, 59, 59);
            }

            public bool IsDateInFinancialYear(DateTime date, string financialYear)
            {
                string currentFinancialYear = GetFinancialYear(date);

                return string.Equals(
                    currentFinancialYear,
                    financialYear,
                    StringComparison.OrdinalIgnoreCase);
            }
        }
    }

