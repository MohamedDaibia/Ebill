namespace EBill.Models
{
    // One line item dispatched to a customer, as Stock Management would report it for
    // the Create Invoice ("connected with inventory") flow - see IDispatchService.
    //
    // PLACEHOLDER DATA SOURCE: Stock Management doesn't yet expose an API for
    // "dispatched items for a customer on a date", so DispatchService currently
    // returns made-up sample rows shaped like this DTO purely so this page can be
    // built and previewed today. Once that endpoint exists, replace
    // DispatchService's implementation with a real StockApiClient call and adjust
    // these fields to match whatever it actually returns.
    public class DispatchedItemDto
    {
        // Stock Management's own id for this dispatched item - carried onto the
        // Product row created from it (see Product.DispatchProductId) so the same
        // dispatch can't be billed twice (unique index in ApplicationDbContext).
        public string DispatchProductId { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string HsnSac { get; set; } = string.Empty;

        public decimal Qty { get; set; }

        public string QtyType { get; set; } = string.Empty;

        public DateTime DispatchedDate { get; set; }

        // Not shown as its own column on the Create Invoice page (by design - only
        // #, Description, HSN/SAC, Qty, Qty Type, Dispatched Date are displayed),
        // but still needed to back the "Calculate amount" button and to price the
        // Product rows actually saved once the invoice is created.
        public decimal Rate { get; set; }

        public decimal Amount => Qty * Rate;
    }
}
