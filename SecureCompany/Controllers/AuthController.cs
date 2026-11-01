using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using SecureCompany.Domain.Entities;
using SecureCompany.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using SecureCompany.Application.Interfaces.Authentication;

namespace SecureCompany.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuth _auth;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            IAuth auth,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            IRefreshTokenService refreshTokenService
            )
        {
            _userManager = userManager;
            _auth = auth;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _refreshTokenService = refreshTokenService;
        }

        [AllowAnonymous]
        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            ApplicationUser? existingEmail = await _userManager.FindByEmailAsync(request.Email);
            if (existingEmail is not null)
            {
                return Conflict(new
                {
                    message = "Email is already registered."
                });
            }

            ApplicationUser? existingUserName = await _userManager.FindByNameAsync(request.UserName);
            if (existingUserName is not null)
            {
                return Conflict(new
                {
                    message = "Username is already taken."
                });
            }

            ApplicationUser? user = new ApplicationUser
            {
                FullName = request.FullName,
                Email = request.Email,
                UserName = request.UserName,
                Department = request.Department,
                EmailConfirmed = true,

            };

            if (!await _roleManager.RoleExistsAsync("Employee"))
            {
                await _roleManager.CreateAsync(new IdentityRole("Employee"));
            }

            IdentityResult? result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                return BadRequest(new
                {
                    errors = result.Errors.Select(e => new
                    {
                        e.Code,
                        e.Description
                    })
                });
            }

            IdentityResult? roleResult = await _userManager.AddToRoleAsync(user, "Employee");
            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);

                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    message = "Failed to assign default role."
                });
            }

            return StatusCode(StatusCodes.Status201Created, new
            {
                message = "User registered successfully.",
                userId = user.Id,
                userName = user.UserName,
                email = user.Email
            });
        }

        [AllowAnonymous]
        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            ApplicationUser? user = await _userManager.FindByEmailAsync(request.Email);
            if (user is null)
            {
                return Unauthorized(
                    new
                    {
                        message = "Invalid email or password."
                    }
                );
            }

            if (!user.LockoutEnabled) await _userManager.SetLockoutEnabledAsync(user, true);

            var result = await _signInManager.CheckPasswordSignInAsync(
                user,
                request.Password,
                true
            );
            if (result.IsLockedOut)
            {
                return StatusCode(StatusCodes.Status423Locked, new
                {
                    message = "Account is temporarily locked. Try again later."
                });
            }
            if (!result.Succeeded)
            {
                return Unauthorized(
                    new
                    {
                        message = "Invalid email or password."
                    }
                );
            }

            var token = await _auth.GenerateTokenAsync(user);

            /*
            return Ok(
                new
                {
                    message = "Login successful.",
                    accessToken = token.Token,
                    expireAt = token.ExpiresAt
                }
            );
            */
            var accessToken = await _auth.GenerateTokenAsync(user);

            var refreshToken = await _refreshTokenService.CreateAsync(user.Id);

            return Ok(new
            {
                message = "Login successful",

                accessToken = accessToken.Token,
                accessTokenExpiresAt = accessToken.ExpiresAt,

                refreshToken = refreshToken.Token,
                refreshTokenExpiresAt = refreshToken.ExpiresAt
            });
        }

        [AllowAnonymous]
        [HttpPost("Refresh")]
        public async Task<IActionResult> Refresh(
            [FromBody] RefreshTokenRequest request)
        {
            var refreshToken = await _refreshTokenService.RotateAsync(
                request.RefreshToken
            );

            if (refreshToken == null)
            {
                return Unauthorized(new
                {
                    message = "Invalid or expired refresh token."
                });
            }

            var user = await _userManager.FindByIdAsync(
                refreshToken.UserId
            );

            if (user == null)
            {
                return Unauthorized(new
                {
                    message = "User not found."
                });
            }

            var accessToken = await _auth.GenerateTokenAsync(user);

            return Ok(new
            {
                message = "Token refreshed successfully.",

                accessToken = accessToken.Token,
                accessTokenExpiresAt = accessToken.ExpiresAt,

                refreshToken = refreshToken.Token,
                refreshTokenExpiresAt = refreshToken.ExpiresAt
            });
        }

        [AllowAnonymous]
        [HttpPost("Logout")]
        public async Task<IActionResult> Logout(
            [FromBody] RefreshTokenRequest request)
        {
            bool revoked = await _refreshTokenService.RevokeAsync(
                request.RefreshToken
            );

            if (!revoked)
            {
                return BadRequest(new
                {
                    message = "Invalid or inactive refresh token."
                });
            }

            return Ok(new
            {
                message = "Logged out successfully."
            });
        }
    }
}