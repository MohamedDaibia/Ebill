using System.ComponentModel.DataAnnotations;

namespace EBill.Models
{
    // Posted back by Views/Invoice/CreateFromCustomer.cshtml's "Create Invoice" form -
    // one row per dispatched item shown on that page. DispatchProductId/Description/
    // HsnSac/Qty/QtyType/DispatchedDate travel as hidden fields (carrying over
    // exactly what IDispatchService returned for that row); Rate is the one field
    // the user can actually edit (the "Cost/Qty" input), so
    // InvoiceController.CreateFromDispatch uses whatever value they left in it
    // rather than re-querying the dispatch service and overwriting their edit.
    public class DispatchLineItemInput
    {
        public string DispatchProductId { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string HsnSac { get; set; } = string.Empty;

        public decimal Qty { get; set; }

        public string QtyType { get; set; } = string.Empty;

        public DateTime DispatchedDate { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Cost/Qty cannot be negative.")]
        public decimal Rate { get; set; }
    }
}
