namespace EBill.Models
{
    public interface IAuditable
    {
        DateTime CreatedDate { get; set; }
        DateTime UpdatedDate { get; set; }
    }
}
