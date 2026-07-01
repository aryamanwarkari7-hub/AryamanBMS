namespace AryamanBMS.Models
{
    public class InvoiceTrackerViewModel
    {
        public List<InvoiceModel> Invoices { get; set; } = new();

        public List<ClientModel> Clients { get; set; } = new();
    }
}