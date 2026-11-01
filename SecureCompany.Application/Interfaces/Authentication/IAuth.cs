using SecureCompany.Domain.Entities;
using SecureCompany.Application.DTOs;

namespace SecureCompany.Application.Interfaces.Authentication
{
    public interface IAuth
    {
        Task<AuthResponse> GenerateTokenAsync(ApplicationUser user);
    }
}