using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace EBill.Models
{
    public class Invoice : IAuditable
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Customer is required.")]
        public int CustomerId { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public Customer? Customer { get; set; }

        // Set when this invoice was generated from a quotation (see
        // InvoiceController.CreateFromQuotation).
        public int? QuotationId { get; set; }

        [ForeignKey(nameof(QuotationId))]
        public Quotation? Quotation { get; set; }

        [Required(ErrorMessage = "Bill number is required.")]
        [StringLength(50, ErrorMessage = "Bill number cannot exceed 50 characters.")]
        [Display(Name = "Bill Number")]
        public string BillNumber { get; set; } = string.Empty;

        // Display-only formatting of BillNumber as "INV-000006" (six digits,
        // zero-padded) wherever the invoice number is shown to the user. The raw
        // BillNumber column keeps storing the plain number - see
        // InvoiceController.AssignNextBillNumberAsync - so the auto-numbering
        // counter in Settings still just does simple integer math. Falls back to
        // the raw value if it's ever something non-numeric (e.g. legacy data),
        // and to an empty string before a number has been assigned.
        [NotMapped]
        public string DisplayBillNumber
        {
            get
            {
                if (string.IsNullOrWhiteSpace(BillNumber))
                {
                    return string.Empty;
                }

                return int.TryParse(BillNumber, out var n)
                    ? $"INV-{n:D6}"
                    : BillNumber;
            }
        }

        [Required(ErrorMessage = "Date is required.")]
        public DateTime Date { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Total amount cannot be negative.")]
        [Display(Name = "Total Amount")]
        public decimal TotalAmount { get; set; }

        // Extra charge on top of the line items and tax (e.g. freight, packing,
        // installation), entered on the Products page. It is NOT taxed - it is added
        // to the Grand Total after CGST/SGST. AdditionalCostLabel is the optional
        // name shown next to it on the invoice (falls back to "Additional Cost").
        [Range(0, double.MaxValue, ErrorMessage = "Additional cost cannot be negative.")]
        [Display(Name = "Additional Cost")]
        public decimal AdditionalCost { get; set; }

        [StringLength(100, ErrorMessage = "Additional cost description cannot exceed 100 characters.")]
        [Display(Name = "Additional Cost Description")]
        public string? AdditionalCostLabel { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Amount paid cannot be negative.")]
        [Display(Name = "Amount Paid")]
        public decimal AmountPaid { get; set; }

        [Required(ErrorMessage = "Payment status is required.")]
        [Display(Name = "Payment Status")]
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

        public bool Emailed { get; set; }

        public DateTime CreatedDate { get; set; } = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));

        public DateTime UpdatedDate { get; set; } = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));

        [ValidateNever]
        public ICollection<Product> Products { get; set; } = new List<Product>();

        [ValidateNever]
        public ICollection<Tax> Taxes { get; set; } = new List<Tax>();
    }
}
