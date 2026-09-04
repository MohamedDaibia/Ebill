using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EBill.Models
{
    public class Tax : IAuditable
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Customer is required.")]
        public int CustomerId { get; set; }

        [ForeignKey(nameof(CustomerId))]
        public Customer? Customer { get; set; }

        public int? InvoiceId { get; set; }

        [ForeignKey(nameof(InvoiceId))]
        public Invoice? Invoice { get; set; }

        [Display(Name = "CGST")]
        [Range(0, double.MaxValue, ErrorMessage = "CGST cannot be negative.")]
        public decimal Cgst { get; set; }

        [Display(Name = "SGST")]
        [Range(0, double.MaxValue, ErrorMessage = "SGST cannot be negative.")]
        public decimal Sgst { get; set; }

        [Display(Name = "Total Tax")]
        [Range(0, double.MaxValue, ErrorMessage = "Total tax cannot be negative.")]
        public decimal TotalTax { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime UpdatedDate { get; set; }
    }
}
