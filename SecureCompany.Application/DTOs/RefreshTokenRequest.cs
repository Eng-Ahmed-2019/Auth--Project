using System.ComponentModel.DataAnnotations;

namespace SecureCompany.Application.DTOs
{
    public class RefreshTokenRequest
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }
}