namespace EBill.Models
{
    // Options for the "Pay By" dropdown on the ledger's Add Debit form.
    public static class PaymentModes
    {
        public const string Cash = "Cash";

        public static readonly string[] All =
        {
            "Cash", "Google Pay", "Phone Pay", "Paytm", "Bank Transfer", "Other"
        };
    }
}
