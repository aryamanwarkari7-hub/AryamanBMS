namespace AryamanBMS.ViewModels
{
    public class BirthdayLeaveBalanceViewModel
    {
        public decimal Entitlement { get; set; } = 1m;

        public decimal Used { get; set; }

        public decimal Available =>
            Math.Max(0m, Entitlement - Used);

        public string Status =>
            Available > 0
                ? "Available"
                : "Used";
    }
}