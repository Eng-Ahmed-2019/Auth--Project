using ecureCompany.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace SecureCompany.Domain.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }
        public string? Department { get; set; }

        public ICollection<RefreshToken> RefreshTokens { get; set; }
            = new List<RefreshToken>();
    }
}