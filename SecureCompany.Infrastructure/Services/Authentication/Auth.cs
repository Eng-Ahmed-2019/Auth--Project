using System.Text;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using SecureCompany.Domain.Entities;
using Microsoft.IdentityModel.Tokens;
using SecureCompany.Application.DTOs;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Configuration;
using SecureCompany.Application.Interfaces.Authentication;

namespace SecureCompany.Infrastructure.Services.Authentication
{
    public class Auth : IAuth
    {
        private readonly IConfiguration _configuration;
        private readonly UserManager<ApplicationUser> _userManager;

        public Auth
            (UserManager<ApplicationUser> userManager,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _configuration = configuration;
        }

        public async Task<AuthResponse> GenerateTokenAsync(ApplicationUser user)
        {
            string jwtKey = _configuration["Jwt:Key"] ??
                throw new InvalidOperationException("JWT Key is missing.");

            string issuer = _configuration["Jwt:Issuer"] ??
                throw new InvalidOperationException("Issuer is missing.");

            string audience = _configuration["Jwt:Audience"] ??
                throw new InvalidOperationException("audience is missing.");

            /*
            bool isExpireTime = int.TryParse(
                _configuration["Jwt:ExpirationInMinutes"],
                out int expireTime
            );
            if (!isExpireTime) throw new InvalidOperationException("Expiration time is missing");
            */

            int expireTime = _configuration.GetValue<int>("Jwt:ExpirationInMinutes");
            if (expireTime <= 0)
                throw new InvalidOperationException("Invalid JWT expiration time.");
            DateTime expireAt = DateTime.UtcNow.AddMinutes(expireTime);

            IList<string>? roles = await _userManager.GetRolesAsync(user);
            IList<Claim>? userClaims = await _userManager.GetClaimsAsync(user);
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub,user.Id),
                new(JwtRegisteredClaimNames.Email,user.Email ?? ""),
                //new(ClaimTypes.Name,user.UserName!),
                new("name",user.UserName ?? ""),
                new("FullName", user.FullName ?? ""),
                new("Department", user.Department ?? ""),
                new(JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString())
            };
            foreach (var claim in userClaims)
            {
                if (claim.Type == "Permission")
                {
                    claims.Add(new Claim(claim.Type, claim.Value));
                }
            }
            foreach (var role in roles)
            {
                //claims.Add(new(ClaimTypes.Role, role));
                claims.Add(new Claim("role", role));
            }

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)
            );

            var credentials = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256
            );

            JwtSecurityToken token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expireAt,
                signingCredentials: credentials
            );

            string? tokenResponse = new JwtSecurityTokenHandler().WriteToken(token);

            return new AuthResponse
            {
                Token = tokenResponse,
                ExpiresAt = expireAt
            };
        }
    }
}