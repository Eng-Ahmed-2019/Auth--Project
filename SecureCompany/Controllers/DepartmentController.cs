using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace SecureCompany.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class DepartmentController : ControllerBase
    {
        private readonly IAuthorizationService _authorizationService;

        public DepartmentController(
            IAuthorizationService authorizationService)
        {
            _authorizationService = authorizationService;
        }

        [HttpGet("SameDepartment/{departmentName}")]
        public async Task<IActionResult> GetDepartment(
            string departmentName)
        {
            string? userDepartment =
                User.FindFirst("Department")?.Value;

            var result = await _authorizationService.AuthorizeAsync(
                User,
                departmentName,
                "SameDepartment"
            );

            if (!result.Succeeded)
            {
                return Forbid();
            }

            return Ok(new
            {
                message = "You can access this department.",
                userDepartment,
                requestedDepartment = departmentName
            });
        }
    }
}