using System.ComponentModel.DataAnnotations;

namespace SecureCompany.Application.DTOs
{
    public class CreateLeaveRequestRequest
    {
        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        [StringLength(500, MinimumLength = 5)]
        public string Reason { get; set; } = string.Empty;
    }
}