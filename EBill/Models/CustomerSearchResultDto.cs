namespace EBill.Models
{
    // Shape returned by CustomerController.Search — deliberately thin (no address/GSTIN),
    // since this is consumed by Stock Management's Dispatch To autocomplete, not a full
    // customer record.
    public class CustomerSearchResultDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
    }
}
