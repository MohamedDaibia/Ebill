using System.ComponentModel.DataAnnotations;

namespace EBill.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }

        // Set when an external app (e.g. Stock Management's "Sync Ebill" button)
        // sent the user here to connect — carried through the form as a hidden
        // field so it survives the POST. See EbillConnectService.IsAllowedReturnUrl.
        public string? ReturnUrl { get; set; }

        // The connecting app's own company id (e.g. Stock Management's), carried
        // alongside ReturnUrl the same way, so EbillConnectService can save it onto
        // this Ebill company once login succeeds - see Company.StockCompanyId.
        public string? StockCompanyId { get; set; }
    }
}
