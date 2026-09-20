namespace EBill.Models
{
    // One line item dispatched to a customer, as Stock Management reports it for the
    // Create Invoice ("connected with inventory") flow - see IDispatchService/
    // DispatchService, which map this from StockDispatchResultDto (Stock Management's
    // GET api/Dispatch/GetDispatchesForCustomer response).
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

        // Set when this dispatch has already been billed onto a Product row
        // (DispatchProductId is unique - see ApplicationDbContext) - the row still
        // shows on the Create Invoice page, but as read-only history with a link to
        // InvoiceId/InvoiceNumber instead of an editable line the "Create Invoice"
        // button would bill again.
        public bool IsInvoiced { get; set; }

        public int? InvoiceId { get; set; }

        public string? InvoiceNumber { get; set; }
    }
}
