using System.Net.Http.Json;
using EBill.Models;

namespace EBill.Services
{
    // Talks to Stock Management's token-exchange endpoint (that app's
    // Controllers/TokenController.cs), using the refresh token + Stock company id
    // stored in this company's Settings row (see Models/Settings.cs, populated by
    // StockSyncController.RegisterRefreshToken) - lets Ebill call Stock Management's
    // APIs on the company's behalf without an interactive login each time. Base
    // address is configured from "Stock:BaseUrl" (see Program.cs's AddHttpClient
    // registration). Mirrors Stock Management's own EbillApiClient.
    public class StockApiClient
    {
        private readonly HttpClient _httpClient;

        public StockApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // Returns null if Stock Management rejected the refresh token (expired,
        // revoked, or the Settings row is stale) - callers should treat that as
        // "needs reconnecting" (re-run Sync Ebill from Stock Management's side).
        public async Task<StockAccessTokenResultDto?> GetAccessTokenAsync(string stockCompanyId, string refreshToken)
        {
            var response = await _httpClient.PostAsJsonAsync("api/Token/Refresh", new
            {
                companyId = stockCompanyId,
                refreshToken
            });

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<StockAccessTokenResultDto>();
        }
    }
}
