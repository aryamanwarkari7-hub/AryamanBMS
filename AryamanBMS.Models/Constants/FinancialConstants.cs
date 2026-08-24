namespace AryamanBMS.Models
{

    public static class FinancialConstants
    {
        public const int FinancialYearStartMonth = 4; // April

        public const string ProposalPrefix = "PROP";
        public const string PurchaseOrderPrefix = "PO";
        public const string InvoicePrefix = "INV";
        public const string ExpenseVoucherPrefix = "EXP";

        public const string PfChallanPrefix = "PF";
        public const string EsicChallanPrefix = "ESIC";
        public const string PtChallanPrefix = "PT";

        public static class StatutoryStatus
        {
            public const string Pending = "Pending";
            public const string Filed = "Filed";
            public const string Paid = "Paid";
        }

        public static class NoticeDepartment
        {
            public const string GST = "GST";
            public const string PF = "PF";
            public const string ESIC = "ESIC";
            public const string IncomeTax = "IncomeTax";
            public const string Labour = "Labour";
            public const string ROC = "ROC";
            public const string Other = "Other";
        }

        public static class NoticeStatus
        {
            public const string Open = "Open";
            public const string Replied = "Replied";
            public const string Closed = "Closed";
            public const string Escalated = "Escalated";
        }


        public static class ProposalStatus
        {
            public const string Draft = "Draft";
            public const string Submitted = "Submitted";
            public const string Approved = "Approved";
            public const string Rejected = "Rejected";
            public const string Converted = "Converted";
            public const string Cancelled = "Cancelled";
        }

        public static class PurchaseOrderStatus
        {
            public const string Draft = "Draft";
            public const string Approved = "Approved";
            public const string Cancelled = "Cancelled";
        }

        public static class InvoiceStatus
        {
            public const string Draft = "Draft";
            public const string Generated = "Generated";
            public const string Sent = "Sent";
            public const string Pending = "Pending";
            public const string PartiallyPaid = "Partially Paid";
            public const string Paid = "Paid";
            public const string Cancelled = "Cancelled";
        }

        public static class PaymentStatus
        {
            public const string Unpaid = "Unpaid";
            public const string PartiallyPaid = "Partially Paid";
            public const string Paid = "Paid";
            public const string Overdue = "Overdue";
        }

        public static class ExpenseVoucherStatus
        {
            public const string Draft = "Draft";
            public const string Submitted = "Submitted";
            public const string Approved = "Approved";
            public const string Posted = "Posted";
            public const string Rejected = "Rejected";
            public const string Reversed = "Reversed";
        }

        public static class GstSnapshotStatus
        {
            public const string Draft = "Draft";
            public const string Calculated = "Calculated";
            public const string Verified = "Verified";
            public const string Filed = "Filed";
            public const string Locked = "Locked";
        }
    }

}
