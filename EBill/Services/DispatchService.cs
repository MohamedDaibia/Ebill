using EBill.Data;
using EBill.Models;
using Microsoft.EntityFrameworkCore;

namespace EBill.Services
{
    // Looks up items Stock Management dispatched to a given customer on a given date,
    // for the "Create Invoice" page (Views/Invoice/CreateFromCustomer.cshtml). Calls
    // Stock Management's api/Dispatch/GetDispatchesForCustomer via StockApiClient,
    // using this company's saved Settings (StockCompanyId/RefreshToken - see
    // StockSyncController.RegisterRefreshToken) to get an access token first, same
    // pattern EbillConnectService's doc comments describe for the reverse direction.
    public class DispatchService : IDispatchService
    {
        // Only items still in this status (i.e. not yet restocked/cancelled on Stock
        // Management's side) are offered for invoicing here.
        private const string DispatchedStatus = "Dispatched";

        private readonly ApplicationDbContext _db;
        private readonly StockApiClient _stockApiClient;

        public DispatchService(ApplicationDbContext db, StockApiClient stockApiClient)
        {
            _db = db;
            _stockApiClient = stockApiClient;
        }

        public async Task<List<DispatchedItemDto>> GetDispatchedItemsAsync(int companyId, int customerId, DateTime date)
        {
            var customer = await _db.Customers
                .FirstOrDefaultAsync(c => c.Id == customerId && c.CompanyId == companyId);

            if (customer is null)
            {
                return new List<DispatchedItemDto>();
            }

            var settings = await _db.Settings.FirstOrDefaultAsync(s => s.CompanyId == companyId);

            // Not connected to Stock Management (or the link hasn't finished the
            // RegisterRefreshToken handshake yet) - nothing to show. Same condition
            // CustomersController.Index checks before it even shows the "Create
            // Invoice" button, so this is mostly a defensive fallback.
            if (settings is null || !settings.IsSynced ||
                string.IsNullOrWhiteSpace(settings.StockCompanyId) || string.IsNullOrWhiteSpace(settings.RefreshToken))
            {
                return new List<DispatchedItemDto>();
            }

            var tokenResult = await _stockApiClient.GetAccessTokenAsync(settings.StockCompanyId, settings.RefreshToken);
            if (tokenResult is null)
            {
                // Refresh token rejected (expired/revoked) - StockApiClient's own doc
                // comment calls this "needs reconnecting"; nothing to show until the
                // company reconnects from Stock Management's side.
                return new List<DispatchedItemDto>();
            }

            var dispatches = await _stockApiClient.GetDispatchesForCustomerAsync(
                settings.StockCompanyId, tokenResult.AccessToken, customerId, date, DispatchedStatus);

            if (dispatches.Count == 0)
            {
                return new List<DispatchedItemDto>();
            }

            // DispatchProductId is unique per Product row (ApplicationDbContext), so a
            // dispatch already billed onto one can't be billed again - InvoiceController.
            // CreateFromDispatch still re-checks this at save time as a last-resort guard
            // against a race, but here it's used to ANNOTATE those rows (IsInvoiced +
            // which invoice) rather than hide them, so the operator can see what's
            // already been billed for this customer/date instead of it silently
            // disappearing from the list.
            var dispatchIds = dispatches.Select(d => d.DispatchId.ToString()).ToList();
            var invoicedLookup = await _db.Products
                .Where(p => p.DispatchProductId != null && dispatchIds.Contains(p.DispatchProductId))
                .Select(p => new { p.DispatchProductId, p.InvoiceId, p.Rate, BillNumber = p.Invoice != null ? p.Invoice.BillNumber : null })
                .ToDictionaryAsync(p => p.DispatchProductId!, p => p);

            return dispatches
                .Select(d =>
                {
                    invoicedLookup.TryGetValue(d.DispatchId.ToString(), out var invoiced);

                    return new DispatchedItemDto
                    {
                        // Stock Management's own id for this dispatch EVENT (not
                        // ProductId, which repeats every time the same product is
                        // dispatched again) - see StockDispatchResultDto's doc comment.
                        DispatchProductId = d.DispatchId.ToString(),
                        Description = string.IsNullOrWhiteSpace(d.BrandName) ? d.ProductName : $"{d.ProductName} ({d.BrandName})",
                        // Stock Management's "Product Code" (Products.ProductCode /
                        // ProductDispatch.ProductCode, up to 50 chars there) is used as
                        // the HSN/SAC code - still editable on the Create Invoice page in
                        // case it needs correcting. Clamped to 20 chars since that's all
                        // Product.HsnSac's column allows (StringLength(20)/nvarchar(20)) -
                        // without this, a longer code would fail the whole invoice save
                        // with a SQL truncation error.
                        HsnSac = string.IsNullOrEmpty(d.ProductCode)
                            ? string.Empty
                            : d.ProductCode[..Math.Min(20, d.ProductCode.Length)],
                        Qty = d.Quantity,
                        QtyType = d.QtyType,
                        DispatchedDate = d.DispatchDate,
                        // Stock Management doesn't carry a selling price - for a
                        // not-yet-invoiced row this starts at 0 and the "Cost/Qty" input
                        // on the Create Invoice page is where the operator sets it before
                        // the invoice is created. For an already-invoiced row, show what
                        // was actually billed instead (from the Product row it created).
                        Rate = invoiced?.Rate ?? 0m,
                        IsInvoiced = invoiced is not null,
                        InvoiceId = invoiced?.InvoiceId,
                        InvoiceNumber = invoiced?.BillNumber,
                    };
                })
                .OrderBy(d => d.DispatchedDate)
                .ToList();
        }
    }
}
