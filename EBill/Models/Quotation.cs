using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace EBill.Models
{
    // Saved the same way as an Invoice, minus tax and payment tracking - a
    // quotation is a price estimate, not a bill that gets paid off.
    public class Quotation : IAuditable
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Customer is required.")]
        public int CustomerId { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public Customer? Customer { get; set; }

        [Required(ErrorMessage = "Quotation number is required.")]
        [StringLength(50, ErrorMessage = "Quotation number cannot exceed 50 characters.")]
        [Display(Name = "Quotation Number")]
        public string QuotationNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Date is required.")]
        public DateTime Date { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Total amount cannot be negative.")]
        [Display(Name = "Total Amount")]
        public decimal TotalAmount { get; set; }

        public DateTime CreatedDate { get; set; } = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));

        public DateTime UpdatedDate { get; set; } = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));

        [ValidateNever]
        public ICollection<Product> Products { get; set; } = new List<Product>();

        // Invoices generated from this quotation (see InvoiceController.CreateFromQuotation).
        [ValidateNever]
        public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    }
}
