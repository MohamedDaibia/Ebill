namespace EBill.Models
{
    // View model for Views/Invoice/CreateFromCustomer.cshtml - the "Create Invoice"
    // page reached from a Customer List row (only shown once this Ebill company is
    // connected to Stock Management - see CustomersController.Index). Bundles the
    // customer being invoiced, the selected dispatch date, and the dispatched items
    // found for that customer/date so the view doesn't need multiple models.
    public class CreateInvoiceFromDispatchViewModel
    {
        public Customer Customer { get; set; } = null!;

        public DateTime SelectedDate { get; set; }

        // Latest date the filter will accept - yesterday, since today's dispatches
        // aren't final yet (see InvoiceController.CreateFromCustomer). Drives the
        // date input's max attribute so the picker itself can't offer today/future.
        public DateTime MaxSelectableDate { get; set; }

        // True when the requested date (e.g. from a hand-edited URL) was today or
        // later and got silently clamped down to MaxSelectableDate - lets the view
        // explain why the date shown differs from what was asked for.
        public bool DateWasAdjusted { get; set; }

        public List<DispatchedItemDto> Items { get; set; } = new();
    }
}
