using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SecureCompany.Domain.Data;
using ecureCompany.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using SecureCompany.Application.DTOs;
using Microsoft.AspNetCore.Authorization;

namespace SecureCompany.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class LeaveRequestsController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IAuthorizationService _authorizationService;

        public LeaveRequestsController(ApplicationDbContext dbContext,
            IAuthorizationService authorizationService)
        {
            _dbContext = dbContext;
            _authorizationService = authorizationService;
        }

        [HttpPost("CreateRequest")]
        public async Task<IActionResult> CreateLeaveRequest(
            [FromBody] CreateLeaveRequestRequest request)
        {
            var userId = User.FindFirstValue("sub");
            if (userId is null)
            {
                return Unauthorized(new
                {
                    message = "User ID not found in claims."
                });
            }

            if (request.StartDate.Date < DateTime.UtcNow.Date)
            {
                return BadRequest(new
                {
                    message = "Start date cannot be in the past."
                });
            }
            if (request.EndDate.Date < request.StartDate.Date)
            {
                return BadRequest(new
                {
                    message = "End date cannot be before start date."
                });
            }

            var myRequest = new LeaveRequest
            {
                UserId = userId,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Reason = request.Reason,
                Status = LeaveRequestStatus.Pending
            };

            _dbContext.LeaveRequests.Add(myRequest);
            await _dbContext.SaveChangesAsync();

            return CreatedAtAction(
            nameof(GetMyRequests),
            new { },
            new
            {
                message = "Leave request created successfully",
                ID = myRequest.Id,
                StartFrom = myRequest.StartDate,
                EndAt = myRequest.EndDate,
                Status = myRequest.Status.ToString()
            });
        }

        [HttpGet("MyLeaveRequests")]
        public async Task<IActionResult> GetMyRequests()
        {
            var userId = User.FindFirstValue("sub");
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var myRequests = await _dbContext.LeaveRequests
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new
                {
                    ID = r.Id,
                    StartFrom = r.StartDate,
                    EndAt = r.EndDate,
                    LeaveReason = r.Reason,
                    Status = r.Status.ToString(),
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();

            return Ok(myRequests);
        }

        [HttpGet("GetRequests/{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var leaveRequest = await _dbContext.LeaveRequests
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == id);
            if (leaveRequest is null)
            {
                return NotFound(new
                {
                    message = "Leave request not found."
                });
            }

            AuthorizationResult? authorizationResult = await _authorizationService.AuthorizeAsync(
                User,
                leaveRequest,
                "CanViewLeaveRequests"
            );
            if (!authorizationResult.Succeeded)
            {
                return NotFound(new
                {
                    message = "Leave request not found."
                });
            }

            return Ok(new
            {
                ID = leaveRequest.Id,
                UserId = leaveRequest.UserId,
                StartFrom = leaveRequest.StartDate,
                EndAt = leaveRequest.EndDate,
                LeaveReason = leaveRequest.Reason,
                Status = leaveRequest.Status.ToString(),
                CreatedAt = leaveRequest.CreatedAt
            });
        }

        [HttpPut("Approve/{id:int}")]
        public async Task<IActionResult> ApproveRequest(int id)
        {
            var leaveRequest = await _dbContext.LeaveRequests
                .FirstOrDefaultAsync(r => r.Id == id);
            if (leaveRequest is null)
            {
                return NotFound(new
                {
                    message = "Leave request not found."
                });
            }

            var authorizationResult = await _authorizationService.AuthorizeAsync(
                User,
                leaveRequest,
                "CanApproveLeaveRequest"
            );
            if (!authorizationResult.Succeeded)
            {
                return Forbid();
            }

            if (leaveRequest.Status != LeaveRequestStatus.Pending)
            {
                return BadRequest(new
                {
                    message = "Only pending requests can be approved."
                });
            }

            leaveRequest.Status = LeaveRequestStatus.Approved;
            await _dbContext.SaveChangesAsync();

            return Ok(new
            {
                message = "Leave request approved successfully.",
                ID = leaveRequest.Id,
                Status = leaveRequest.Status.ToString()
            });
        }

        [HttpPut("Reject/{id:int}")]
        public async Task<IActionResult> Reject(int id)
        {
            var leaveRequest = await _dbContext.LeaveRequests
                .FirstOrDefaultAsync(x => x.Id == id);
            if (leaveRequest is null)
            {
                return NotFound(new
                {
                    message = "Leave request not found."
                });
            }

            var authorizationResult =
                await _authorizationService.AuthorizeAsync(
                    User,
                    leaveRequest,
                    "CanApproveLeaveRequest"
                );
            if (!authorizationResult.Succeeded)
            {
                return Forbid();
            }

            if (leaveRequest.Status != LeaveRequestStatus.Pending)
            {
                return Conflict(new
                {
                    message = "Only pending requests can be rejected."
                });
            }

            leaveRequest.Status = LeaveRequestStatus.Rejected;

            await _dbContext.SaveChangesAsync();

            return Ok(new
            {
                message = "Leave request rejected successfully.",
                leaveRequest.Id,
                leaveRequest.Status
            });
        }
    }
}