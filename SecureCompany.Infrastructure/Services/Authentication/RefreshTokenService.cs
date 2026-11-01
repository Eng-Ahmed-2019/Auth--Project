using System.Text;
using SecureCompany.Domain.Data;
using System.Security.Cryptography;
using ecureCompany.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using SecureCompany.Application.DTOs;
using Microsoft.Extensions.Configuration;
using SecureCompany.Application.Interfaces.Authentication;

namespace SecureCompany.Infrastructure.Services.Authentication
{
    public class RefreshTokenService : IRefreshTokenService
    {
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;

        public RefreshTokenService(
            ApplicationDbContext context,
            IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        private string GenerateToken()
        {
            byte[] randomBytes = RandomNumberGenerator.GetBytes(64);

            return Convert.ToBase64String(randomBytes);
        }

        private string HashToken(string token)
        {
            return Convert.ToHexString(
                SHA256.HashData(
                    Encoding.UTF8.GetBytes(token)
                )
            );
        }

        private int GetExpirationDays()
        {
            int days = _configuration.GetValue<int>(
                "RefreshToken:ExpirationInDays"
            );

            if (days <= 0)
                throw new InvalidOperationException(
                    "Invalid refresh token expiration time."
                );

            return days;
        }

        public async Task<RefreshTokenResult> CreateAsync(string userId)
        {
            string token = GenerateToken();
            string tokenHash = HashToken(token);

            DateTime expiresAt = DateTime.UtcNow.AddDays(
                GetExpirationDays()
            );

            var refreshToken = new RefreshToken
            {
                TokenHash = tokenHash,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = expiresAt
            };

            _context.RefreshTokens.Add(refreshToken);

            await _context.SaveChangesAsync();

            return new RefreshTokenResult
            {
                Token = token,
                UserId = userId,
                ExpiresAt = expiresAt
            };
        }

        public async Task<RefreshTokenResult?> RotateAsync(string token)
        {
            string tokenHash = HashToken(token);

            using var transaction =
                await _context.Database.BeginTransactionAsync();

            var oldToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(x => x.TokenHash == tokenHash);

            if (oldToken == null || !oldToken.IsActive)
                return null;

            // Generate a new refresh token
            string newToken = GenerateToken();
            string newTokenHash = HashToken(newToken);

            DateTime now = DateTime.UtcNow;

            // Revoke old token
            oldToken.RevokedAt = now;
            oldToken.ReplacedByTokenHash = newTokenHash;

            // Create new token
            DateTime expiresAt = now.AddDays(GetExpirationDays());

            var newRefreshToken = new RefreshToken
            {
                TokenHash = newTokenHash,
                UserId = oldToken.UserId,
                CreatedAt = now,
                ExpiresAt = expiresAt
            };

            _context.RefreshTokens.Add(newRefreshToken);

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            return new RefreshTokenResult
            {
                Token = newToken,
                UserId = oldToken.UserId,
                ExpiresAt = expiresAt
            };
        }

        public async Task<bool> RevokeAsync(string token)
        {
            string tokenHash = HashToken(token);

            var refreshToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(x => x.TokenHash == tokenHash);

            if (refreshToken == null || !refreshToken.IsActive)
                return false;

            refreshToken.RevokedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return true;
        }
    }
}