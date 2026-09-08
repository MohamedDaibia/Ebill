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

        public DateTime CreatedDate { get; set; }

        public DateTime UpdatedDate { get; set; }

        public static readonly string[] QtyTypes =
        {
            "Nos", "Pcs", "Box", "Kg", "Gram","Sq ft", "Litre", "Meter",
            "Set", "Dozen", "Pack", "Bag", "Pair", "Roll", "Bundle"
        };
    }
}
