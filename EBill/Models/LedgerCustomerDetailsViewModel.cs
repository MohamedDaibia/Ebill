namespace EBill.Models
{
    public class LedgerEntryRow
    {
        public Guid Id { get; set; }
        public int? InvoiceId { get; set; }
        public DateTime Date { get; set; }
        public bool IsDebit { get; set; }
        public string? Comments { get; set; }
        public string? By { get; set; }
        public string? PayBy { get; set; }
        public string? InvoiceNumber { get; set; }
        public decimal Credited { get; set; }
        public decimal Debited { get; set; }
    }

    public class InvoiceOption
    {
        public int Id { get; set; }
        public string Label { get; set; } = string.Empty;
    }

    // Everything the Ledger > Customer Details page shows.
    public class LedgerCustomerDetailsViewModel
    {
        public int CustomerId { get; set; }
        public string CustomerNumber { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Gstin { get; set; }
        public string? Address { get; set; }
        public string? District { get; set; }
        public string? State { get; set; }

        public decimal TotalCredited { get; set; }
        public decimal TotalDebited { get; set; }
        public decimal TotalBalance { get; set; }

        public List<LedgerEntryRow> Entries { get; set; } = new();

        // This customer's invoices, for the Add Debit "Invoice" dropdown.
        public List<InvoiceOption> Invoices { get; set; } = new();

        // Saved embed URL, or a search-on-address fallback; null when there's no address.
        public string? MapEmbedUrl { get; set; }
        public string? MapOpenUrl { get; set; }
        public string? SavedMapLink { get; set; }
    }
}
