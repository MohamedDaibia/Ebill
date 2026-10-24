using System.ComponentModel.DataAnnotations;

namespace EBill.Models
{
    public static class UserRoles
    {
        public const string Admin = "Admin";
        public const string Accountant = "Accountant";
        public const string Sales = "Sales";

        public static readonly string[] All = { Admin, Accountant, Sales };
    }

    // A sub-user of a Company (the company account itself is always the Admin).
    // Logs in with Email/Password like the company does; all data stays scoped to
    // CompanyId, so existing controllers keep working off the same NameIdentifier claim.
    public class AppUser
    {
        public int Id { get; set; }

        public int CompanyId { get; set; }
        public Company? Company { get; set; }

        [Required, StringLength(200, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(256)]
        public string Email { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        [Required, StringLength(20)]
        public string Role { get; set; } = UserRoles.Sales;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
