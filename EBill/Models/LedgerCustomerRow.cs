namespace EBill.Models
{
    // One row of the Ledger page: a customer plus their running totals from LedgerBalance
    // (all zero for a customer that has no ledger entries yet).
    public class LedgerCustomerRow
    {
        public int CustomerId { get; set; }
        public string Name { get; set; } = string.Empty;

        public string? District { get; set; } = string.Empty;

        public string? State { get; set; } = string.Empty;
        public string? Address { get; set; }
        public decimal TotalCredited { get; set; }
        public decimal TotalDebited { get; set; }
        public decimal Balance { get; set; }
    }
}
