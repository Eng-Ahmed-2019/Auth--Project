using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace SecureCompany.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EmployeesController : ControllerBase
    {
        [HttpGet("View")]
        [Authorize(Policy = "CanViewEmployees")]
        public IActionResult CanView()
        {
            return Ok(
                new
                {
                    Message = "You have permission to view employees."
                }
            );
        }

        [HttpGet("Create")]
        [Authorize(Policy = "CanCreateEmployees")]
        public IActionResult CanCreate()
        {
            return Ok(
                new
                {
                    Message = "You have permission to create employees."
                }
            );
        }
    }
}