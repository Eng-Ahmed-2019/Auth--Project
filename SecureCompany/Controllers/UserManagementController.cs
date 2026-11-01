using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using SecureCompany.Domain.Entities;
using SecureCompany.Application.DTOs;
using Microsoft.AspNetCore.Authorization;

namespace SecureCompany.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize(Roles = "Admin")]
    public class UserManagementController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UserManagementController
            (UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [HttpPost("{userId}/role/{role}")]
        public async Task<IActionResult> AssignRole(string userId, string role)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
            {
                return NotFound(new
                {
                    message = "User not found."
                });
            }

            var allowedRoles = new[] { "Admin", "HR", "Manager" };
            if (!allowedRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
            {
                return BadRequest(new
                {
                    message = "Invalid role."
                });
            }

            var result = await _userManager.AddToRoleAsync(user, role);
            if (!result.Succeeded)
            {
                return BadRequest(new
                {
                    errors = result.Errors.Select(e => e.Description)
                });
            }

            return Ok(new
            {
                message = $"Role {role} assigned successfully."
            });
        }

        [HttpPost("{userId}/claims")]
        public async Task<IActionResult> AddClaim
            (string userId, [FromBody] AddClaimRequest request)
        {
            ApplicationUser? user = await _userManager.FindByIdAsync(userId);
            if (user is null)
            {
                return NotFound(new
                {
                    message = "User not found."
                });
            }

            var allowedPermisions = new[]
            {
                "Employees.View",
                "Employees.Create"
            };
            if (request.Type != "Permission" || !allowedPermisions.Contains(request.Value))
            {
                return BadRequest(new
                {
                    message = "Invalid claim type or permission."
                });
            }

            Claim? claim = new Claim(request.Type, request.Value);

            var result = await _userManager.AddClaimAsync(user, claim);
            if (!result.Succeeded)
            {
                return BadRequest(new
                {
                    errors = result.Errors.Select(e => e.Description)
                });
            }

            return Ok(new
            {
                message = "Claim added successfully."
            });
        }
    }
}