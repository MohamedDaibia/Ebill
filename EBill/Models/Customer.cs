using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace EBill.Models
{
    public class Customer : IAuditable
    {
        public int Id { get; set; }

        // Business-facing identifier, generated server-side (e.g. CUST-00001).
        [ValidateNever]
        public string CustomerNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Customer name is required.")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Customer name must be between 2 and 200 characters.")]
        [Display(Name = "Customer Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters.")]
        public string? Address { get; set; }

        [RegularExpression(@"^(\+91[\s-]?)?[6-9]\d{4}[\s-]?\d{5}$",
        ErrorMessage = "Enter a valid 10-digit mobile number (optionally prefixed with +91).")]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [RegularExpression(@"^\d{2}[A-Z]{5}\d{4}[A-Z]{1}[1-9A-Z]{1}Z[0-9A-Z]{1}$",
            ErrorMessage = "Enter a valid 15-character GSTIN (e.g. 22AAAAA0000A1Z5).")]
        [Display(Name = "GSTIN")]
        public string? Gstin { get; set; }

        [ValidateNever]
        public DateTime CreatedDate { get; set; } = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));

        [ValidateNever]
        public DateTime UpdatedDate { get; set; }  = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));

        [ValidateNever]
        public ICollection<Product> Products { get; set; } = new List<Product>();

        [ValidateNever]
        public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

        [ValidateNever]
        public ICollection<Quotation> Quotations { get; set; } = new List<Quotation>();

        [ValidateNever]
        public ICollection<Tax> Taxes { get; set; } = new List<Tax>();
    }
}
