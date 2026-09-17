using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EBill.Models
{
    public class Product : IAuditable
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Customer is required.")]
        public int CustomerId { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public Customer? Customer { get; set; }

        public int? InvoiceId { get; set; }

        [ForeignKey(nameof(InvoiceId))]
        public Invoice? Invoice { get; set; }

        public int? QuotationId { get; set; }

        [ForeignKey(nameof(QuotationId))]
        public Quotation? Quotation { get; set; }

        // Stock Management's own id for the dispatched item this line item was
        // created from (see IDispatchService/InvoiceController.CreateFromDispatch) -
        // null for a line item added manually (ProductController.Add/AddToQuotation),
        // which never had a dispatch behind it. Unique (see ApplicationDbContext) so
        // the same dispatched item can't be billed onto two different line items.
        [StringLength(100, ErrorMessage = "Dispatch Product Id cannot exceed 100 characters.")]
        [Display(Name = "Dispatch Product Id")]
        public string? DispatchProductId { get; set; }

        [Required(ErrorMessage = "Description is required.")]
        [StringLength(300, ErrorMessage = "Description cannot exceed 300 characters.")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "HSN/SAC is required.")]
        [StringLength(20, ErrorMessage = "HSN/SAC cannot exceed 20 characters.")]
        [Display(Name = "HSN/SAC")]
        public string HsnSac { get; set; } = string.Empty;

        [Required(ErrorMessage = "Quantity is required.")]
        [RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessage = "Quantity must be a positive number.")]
        public string Qty { get; set; } = string.Empty;

        [Required(ErrorMessage = "Quantity type is required.")]
        [StringLength(20, ErrorMessage = "Quantity type cannot exceed 20 characters.")]
        [Display(Name = "Qty Type")]
        public string QtyType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Rate is required.")]
        [Range(0, double.MaxValue, ErrorMessage = "Rate cannot be negative.")]
        public decimal Rate { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Discount cannot be negative.")]
        public decimal Discount { get; set; }

        [Display(Name = "Tax Rate")]
        public GstRate TaxRate { get; set; } = GstRate.Zero;

        [Display(Name = "Total Amount")]
        public decimal TotalAmount { get; set; }

        public DateTime CreatedDate { get; set; } = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));

        public DateTime UpdatedDate { get; set; } = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));

        public static readonly string[] QtyTypes =
        {
            "Nos", "Pcs", "Box", "Kg", "Gram","Sq ft", "Litre", "Meter",
            "Set", "Dozen", "Pack", "Bag", "Pair", "Roll", "Bundle"
        };
    }
}
