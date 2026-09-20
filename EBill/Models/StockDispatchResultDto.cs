namespace EBill.Models
{
    // Mirrors StockManagement.Models.Dto.DispatchResponseDto - the shape returned by
    // Stock Management's GET api/Dispatch/GetDispatchesForCustomer (see
    // StockApiClient.GetDispatchesForCustomerAsync). DispatchId is Stock Management's
    // id for this specific dispatch event (not ProductId, which repeats every time the
    // same product is dispatched again) - that's what becomes Product.DispatchProductId
    // once billed, so the same dispatch can't be invoiced twice.
    public class StockDispatchResultDto
    {
        public Guid DispatchId { get; set; }
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public string BrandName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string QtyType { get; set; } = string.Empty;
        public string DispatchTo { get; set; } = string.Empty;
        public int? CustomerId { get; set; }
        public DateTime DispatchDate { get; set; }
        public string DispatchStatus { get; set; } = string.Empty;
        public string PackType { get; set; } = string.Empty;
        public decimal PackQuantity { get; set; }
        public string DispatchBatchId { get; set; } = string.Empty;
        public bool Restocked { get; set; }
    }
}
