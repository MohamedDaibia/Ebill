using System.Net.Http.Headers;
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

        // Calls Stock Management's api/Dispatch/GetDispatchesForCustomer (that app's
        // DispatchController) with the access token just redeemed above, plus the
        // CompanyId header that endpoint reads for scoping (its token doesn't carry a
        // company-id claim - see that project's JwtTokenService comment). Returns an
        // empty list rather than throwing if Stock Management rejects or errors on the
        // call, so DispatchService can treat "not connected" and "no items" the same
        // way the rest of that class already does.
        public async Task<List<StockDispatchResultDto>> GetDispatchesForCustomerAsync(
            string stockCompanyId, string accessToken, int customerId, DateTime date, string status)
        {
            var url = $"api/Dispatch/GetDispatchesForCustomer?customerId={customerId}&date={date:yyyy-MM-dd}&status={Uri.EscapeDataString(status)}";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Add("CompanyId", stockCompanyId);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return new List<StockDispatchResultDto>();
            }

            return await response.Content.ReadFromJsonAsync<List<StockDispatchResultDto>>() ?? new List<StockDispatchResultDto>();
        }
    }
}
