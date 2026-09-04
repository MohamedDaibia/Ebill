namespace EBill.Models
{
    // Each rate is split evenly between CGST and SGST (e.g. Eighteen = 9% CGST + 9% SGST).
    public enum GstRate
    {
        Zero = 0,
        Five = 5,
        Eighteen = 18,
        TwentyEight = 28
    }
}
