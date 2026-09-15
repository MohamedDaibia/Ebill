namespace EBill.Models
{
    public class RefreshTokenRequestDto
    {
        public string EbillCompanyId { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }
}
