using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace EBill.Models
{
    public class Company
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Company name is required.")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Company name must be between 2 and 200 characters.")]
        [Display(Name = "Company Name")]
        public string CompanyName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(256, ErrorMessage = "Email address cannot exceed 256 characters.")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$",
            ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, and one digit.")]
        [DataType(DataType.Password)]
        [NotMapped]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your password.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        [NotMapped]
        public string ConfirmPassword { get; set; } = string.Empty;

        // Hashed password, populated server-side; never bound directly from a form.
        [ValidateNever]
        public string PasswordHash { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [RegularExpression(@"^(\+91[\s-]?)?[6-9]\d{4}[\s-]?\d{5}$",
            ErrorMessage = "Enter a valid 10-digit mobile number (optionally prefixed with +91).")]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Address is required.")]
        [StringLength(500, MinimumLength = 5, ErrorMessage = "Address must be between 5 and 500 characters.")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "State is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "State must be between 2 and 100 characters.")]
        [RegularExpression(@"^[A-Za-z\s&\-]+$", ErrorMessage = "State can only contain letters, spaces, & and -.")]
        public string State { get; set; } = "Tamil Nadu";

        [Required(ErrorMessage = "District is required.")]
        [Display(Name = "District")]
        public string District { get; set; } = string.Empty;

        public static readonly string[] TamilNaduDistricts =
        {
            "Ariyalur", "Chengalpattu", "Chennai", "Coimbatore", "Cuddalore",
            "Dharmapuri", "Dindigul", "Erode", "Kallakurichi", "Kanchipuram",
            "Kanyakumari", "Karur", "Krishnagiri", "Madurai", "Mayiladuthurai",
            "Nagapattinam", "Namakkal", "Nilgiris", "Perambalur", "Pudukkottai",
            "Ramanathapuram", "Ranipet", "Salem", "Sivaganga", "Tenkasi",
            "Thanjavur", "Theni", "Thoothukudi", "Tiruchirappalli", "Tirunelveli",
            "Tirupathur", "Tiruppur", "Tiruvallur", "Tiruvannamalai", "Tiruvarur",
            "Vellore", "Viluppuram", "Virudhunagar"
        };

        [Required(ErrorMessage = "Country is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Country must be between 2 and 100 characters.")]
        [RegularExpression(@"^[A-Za-z\s&\-]+$", ErrorMessage = "Country can only contain letters, spaces, & and -.")]
        public string Country { get; set; } = "India";

        [Required(ErrorMessage = "Pincode is required.")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Pincode must be a 6-digit number.")]
        [Display(Name = "Pincode")]
        public string Pincode { get; set; } = string.Empty;

        [Required(ErrorMessage = "GSTIN is required.")]
        [RegularExpression(@"^\d{2}[A-Z]{5}\d{4}[A-Z]{1}[1-9A-Z]{1}Z[0-9A-Z]{1}$",
            ErrorMessage = "Enter a valid 15-character GSTIN (e.g. 22AAAAA0000A1Z5).")]
        [Display(Name = "GSTIN")]
        public string Gstin { get; set; } = string.Empty;

        [Display(Name = "Logo")]
        [ValidateNever]
        [NotMapped]
        public IFormFile? LogoFile { get; set; }

        // Relative path (under wwwroot) of the saved logo, set after upload.
        public string? LogoPath { get; set; }

        // "Connect" flow (see EbillConnectService): only a hash of the refresh
        // token handed to an external app (e.g. Stock Management) is stored here —
        // the plaintext value is returned to the caller once, at issue time, and
        // never persisted as-is.
        [ValidateNever]
        public string? RefreshTokenHash { get; set; }

        [ValidateNever]
        public DateTime? RefreshTokenExpiresAt { get; set; }

        // Password-reset flow (AccountController.ForgotPassword/ResetPassword): only
        // a hash of the reset token is stored here — the plaintext value is emailed
        // to the company once, at request time, and never persisted as-is. Same
        // shape as RefreshTokenHash above.
        [ValidateNever]
        public string? PasswordResetTokenHash { get; set; }

        [ValidateNever]
        public DateTime? PasswordResetTokenExpiresAt { get; set; }

        // Where to send the user back after login/registration when this request
        // came from an external app's "connect" flow. Never persisted — validated
        // against AllowedConnectOrigins (see EbillConnectService) before use.
        [ValidateNever]
        [NotMapped]
        public string? ReturnUrl { get; set; }

        // The connecting app's own company id (e.g. Stock Management's), carried
        // through the Register form the same way ReturnUrl is above - never
        // persisted directly on Company. EbillConnectService.IssueTokenAndBuild
        // RedirectUrlAsync writes it onto the Settings row instead (see
        // Models/Settings.cs) once registration succeeds.
        [ValidateNever]
        [NotMapped]
        public string? StockCompanyId { get; set; }

        // The Stock Management "connect" link (its company id + the refresh token it
        // issued so Ebill can call its API) lives in its own Settings row now, same
        // shape as Stock Management's own Settings model - see Models/Settings.cs.
        [ValidateNever]
        public Settings? Settings { get; set; }
    }
}
