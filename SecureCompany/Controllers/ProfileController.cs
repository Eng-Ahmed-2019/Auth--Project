using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SecureCompany.Application.DTOs;
using Microsoft.AspNetCore.Authorization;

namespace SecureCompany.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ProfileController : ControllerBase
    {
        [Authorize(Roles = "Employee")]
        [HttpGet("Employee-GetMe")]
        public IActionResult EmployeeGetMe()
        {
            return Ok(GetUserProfile());
        }

        [Authorize(Roles = "HR")]
        [HttpGet("HR-GetMe")]
        public IActionResult HRGetMe()
        {
            return Ok(GetUserProfile());
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("Admin-GetMe")]
        public IActionResult AdminGetMe()
        {
            return Ok(GetUserProfile());
        }

        [Authorize(Roles = "Manager")]
        [HttpGet("Manager-GetMe")]
        public IActionResult ManagerGetMe()
        {
            return Ok(GetUserProfile());
        }

        private UserProfileResponse GetUserProfile()
        {
            var claims = User.Claims
                .GroupBy(c => c.Type)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(c => c.Value).ToArray()
                );

            return new UserProfileResponse
            {
                UserId = User.FindFirstValue("sub"),
                Email = User.FindFirstValue("email"),
                UserName = User.FindFirstValue("name"),

                FullName = User.FindFirstValue("FullName"),
                Department = User.FindFirstValue("Department"),

                IsAuthenticated = User.Identity?.IsAuthenticated ?? false,

                Roles = User.FindAll("role")
                    .Select(r => r.Value)
                    .ToList(),

                Permissions = User.FindAll("Permission")
                .Select(p => p.Value)
                .ToList(),

                Claims = claims
            };
        }
    }
}