namespace EBill.Models
{
    // A shareable, opaque link to a fixed invoice date range (used by the
    // "Accounting" report on the Invoices list). The date range is captured
    // at creation time so the report stays stable even as new invoices are
    // added later.
    public class AccountingLink
    {
        public Guid Id { get; set; }

        // The day-filter value selected on the Invoices list when this link was
        // created (null = "All Time"). Kept for display purposes only.
        public int? Days { get; set; }

        public DateTime FromDate { get; set; }

        public DateTime ToDate { get; set; }

        public DateTime CreatedDate { get; set; }
    }
}
