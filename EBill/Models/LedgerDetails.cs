using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace EBill.Models
{
    // One row per ledger movement for a customer. Id, CompanyId, CreatedAt/By and
    // UpdatedAt/By come from TableAudit. A row is normally either a credit or a debit,
    // so the unused amount stays 0.
    public class LedgerDetails : TableAudit
    {
        [Required]
        [Display(Name = "Customer ID")]
        public int CustomerId { get; set; }

        [ForeignKey(nameof(CustomerId))]
        [ValidateNever]
        public Customer? Customer { get; set; }

        // Optional link to the invoice this entry relates to.
        [Display(Name = "Invoice ID")]
        public int? InvoiceId { get; set; }

        [ForeignKey(nameof(InvoiceId))]
        [ValidateNever]
        public Invoice? Invoice { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Credited amount cannot be negative.")]
        [Display(Name = "Credited Amount")]
        public decimal CreditedAmount { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Debited amount cannot be negative.")]
        [Display(Name = "Debited Amount")]
        public decimal DebitedAmount { get; set; }

        [StringLength(200)]
        [Display(Name = "Debited By")]
        public string? DebitedBy { get; set; }

        [StringLength(200)]
        [Display(Name = "Credited By")]
        public string? CreditedBy { get; set; }

        [StringLength(500)]
        public string? Comments { get; set; }

        // How the amount was paid (see PaymentModes). Null on rows created before this
        // column existed.
        [StringLength(50)]
        [Display(Name = "Pay By")]
        public string? PayBy { get; set; }
    }
}
