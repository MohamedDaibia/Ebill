using System.Globalization;

namespace EBill.Helpers
{
    // Formats a rupee amount with Indian-style digit grouping (e.g. 1,00,000.00),
    // matching the client-side toLocaleString('en-IN', ...) formatting already used
    // for JS-rendered amounts (see Product/Index.cshtml).
    public static class MoneyFormat
    {
        private static readonly CultureInfo IndianCulture = CultureInfo.GetCultureInfo("en-IN");

        public static string Format(decimal amount)
        {
            return amount.ToString("N2", IndianCulture);
        }
    }
}
