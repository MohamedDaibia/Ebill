namespace EBill.Models
{
    public abstract class TableAudit
    {
        public Guid Id { get; set; } = new Guid();
        public DateTime CreatedAt { get; set; } 
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? UpdatedAt { get; set; } 
        public string? UpdatedBy { get; set; } = string.Empty;
        public int CompanyId { get; set; }
    }
}
