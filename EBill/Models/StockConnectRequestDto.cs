namespace EBill.Models
{
    // Body of the server-to-server call Stock Management makes to
    // StockSyncController.RegisterRefreshToken right after its own ConnectEbill
    // succeeds - hands Ebill the refresh token it'll need to call back into Stock
    // Management's API.
    public class StockConnectRequestDto
    {
        public string StockCompanyId { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }
}
