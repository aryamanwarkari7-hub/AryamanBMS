namespace AryamanBMS.Services.Interface
{
    public interface IFinancialYearService
    {
        string GetFinancialYear(
            DateTime date);

        DateTime GetFinancialYearStartDate(
            DateTime date);

        DateTime GetFinancialYearEndDate(
            DateTime date);

        bool IsDateInFinancialYear(
            DateTime date,
            string financialYear);
    }
}
