using SecureCompany.Application.DTOs;

namespace SecureCompany.Application.Interfaces.Authentication
{
    public interface IRefreshTokenService
    {
        Task<RefreshTokenResult> CreateAsync(string userId);
        Task<RefreshTokenResult?> RotateAsync(string token);
        Task<bool> RevokeAsync(string token);
    }
}