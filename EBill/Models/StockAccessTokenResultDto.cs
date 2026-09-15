namespace EBill.Models
{
    // Shape returned by Stock Management's api/Token/Refresh - see StockApiClient.
    public class StockAccessTokenResultDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public string StockCompanyId { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
    }
}
