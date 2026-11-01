using System.ComponentModel.DataAnnotations;

namespace SecureCompany.Application.DTOs
{
    public class AddClaimRequest
    {
        [Required]
        public string Type { get; set; } = string.Empty;

        [Required]
        public string Value { get; set; } = string.Empty;
    }
}