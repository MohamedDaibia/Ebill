namespace EBill.Models
{
    // Returned by CustomerController.LookupByPhone - used by the Create Invoice / Create
    // Quotation pages to auto-fill Name/Address/GSTIN once a matching customer's phone
    // number is typed in, so a returning customer doesn't have to retype their details.
    // Deliberately a separate, fuller DTO from CustomerSearchResultDto (which stays thin
    // for the Stock Management autocomplete use case).
    public class CustomerLookupResultDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Gstin { get; set; }
    }
}
