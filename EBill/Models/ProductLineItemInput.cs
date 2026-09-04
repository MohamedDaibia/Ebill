using System.ComponentModel.DataAnnotations;

namespace EBill.Models
{
    public class ProductLineItemInput
    {
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
    }
}
