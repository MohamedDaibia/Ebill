using EBill.Models;

namespace EBill.Services
{
    // Looks up items Stock Management has dispatched to a given customer on a given
    // date, for the "Create Invoice" page (Views/Invoice/CreateFromCustomer.cshtml).
    // See DispatchService for the real implementation (calls Stock Management's
    // api/Dispatch/GetDispatchesForCustomer via StockApiClient).
    public interface IDispatchService
    {
        Task<List<DispatchedItemDto>> GetDispatchedItemsAsync(int companyId, int customerId, DateTime date);
    }
}
