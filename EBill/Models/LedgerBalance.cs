using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace EBill.Models
{
    // Running totals per customer (one row per company + customer), kept in step with
    // the LedgerDetails rows. Id, CompanyId and the audit fields come from TableAudit.
    public class LedgerBalance : TableAudit
    {
        [Required]
        [Display(Name = "Customer ID")]
        public int CustomerId { get; set; }

        [ForeignKey(nameof(CustomerId))]
        [ValidateNever]
        public Customer? Customer { get; set; }

        [Display(Name = "Total Credited")]
        public decimal TotalCredited { get; set; }

        [Display(Name = "Total Debited")]
        public decimal TotalDebited { get; set; }

        [Display(Name = "Total Remaining")]
        public decimal TotalRemaining { get; set; }
    }
}
