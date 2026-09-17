using EBill.Data;
using EBill.Models;
using Microsoft.EntityFrameworkCore;

namespace EBill.Services
{
    // PLACEHOLDER IMPLEMENTATION.
    //
    // Stock Management doesn't currently expose an API for "items dispatched to
    // customer X on date Y" - only the token-exchange endpoint StockApiClient talks
    // to exists today (see Settings/Index.cshtml's "Connected" state and
    // StockApiClient.GetAccessTokenAsync). This class returns made-up sample rows so
    // Views/Invoice/CreateFromCustomer.cshtml can be built, previewed and adjusted
    // now, without blocking on that endpoint.
    //
    // TO MAKE THIS REAL once Stock Management adds that endpoint:
    //   1. Inject StockApiClient here instead of (or alongside) ApplicationDbContext.
    //   2. Look up this company's Settings row (StockCompanyId/RefreshToken) and call
    //      StockApiClient.GetAccessTokenAsync to get a Bearer token, same pattern
    //      StockSyncController's comments describe for the reverse direction.
    //   3. Call the real dispatch endpoint (e.g. GET api/Dispatch/ForCustomer?...)
    //      with that token, passing whatever identifies the customer on Stock
    //      Management's side (EBill CustomerId, phone number, or name - confirm
    //      which) and the selected date.
    //   4. Map its response onto DispatchedItemDto and delete the sample data below.
    public class DispatchService : IDispatchService
    {
        private readonly ApplicationDbContext _db;

        public DispatchService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<List<DispatchedItemDto>> GetDispatchedItemsAsync(int companyId, int customerId, DateTime date)
        {
            var customer = await _db.Customers
                .FirstOrDefaultAsync(c => c.Id == customerId && c.CompanyId == companyId);

            if (customer is null)
            {
                return new List<DispatchedItemDto>();
            }

            // Sample rows only - see the class-level comment above. Deterministic per
            // customer/date so the page behaves consistently while this is being
            // reviewed, and so DispatchProductId (unique once actually invoiced -
            // see Product.DispatchProductId) doesn't collide across different
            // customers/dates the way a fixed placeholder id would.
            var datePart = date.ToString("yyyyMMdd");
            return new List<DispatchedItemDto>
            {
                new DispatchedItemDto
                {
                    DispatchProductId = $"DISP-{customerId}-{datePart}-A",
                    Description = "Sample Item A (placeholder)",
                    HsnSac = "1234",
                    Qty = 5,
                    QtyType = "Nos",
                    DispatchedDate = date,
                    Rate = 250m,
                },
                new DispatchedItemDto
                {
                    DispatchProductId = $"DISP-{customerId}-{datePart}-B",
                    Description = "Sample Item B (placeholder)",
                    HsnSac = "5678",
                    Qty = 2,
                    QtyType = "Box",
                    DispatchedDate = date,
                    Rate = 500m,
                },
            };
        }
    }
}
