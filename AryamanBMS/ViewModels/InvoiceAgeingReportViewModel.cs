namespace AryamanBMS.ViewModels
{
    public class InvoiceAgeingReportViewModel
    {
        public decimal TotalOutstanding { get; set; }

        public decimal Bucket0To30 { get; set; }

        public decimal Bucket31To60 { get; set; }

        public decimal Bucket61To90 { get; set; }

        public decimal BucketAbove90 { get; set; }

        public List<InvoiceAgeingRowViewModel> Rows { get; set; } = new();
    }

    public class InvoiceAgeingRowViewModel
    {
        public int InvoiceId { get; set; }

        public string InvoiceNo { get; set; } = string.Empty;

        public string ClientName { get; set; } = string.Empty;

        public DateTime InvoiceDate { get; set; }

        public DateTime? DueDate { get; set; }

        public decimal InvoiceTotal { get; set; }

        public decimal AmountReceived { get; set; }

        public decimal OutstandingBalance { get; set; }

        public int AgeingDays { get; set; }

        public string AgeingBucket { get; set; } = string.Empty;
    }
}