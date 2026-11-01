using System.Security.Claims;
using ecureCompany.Domain.Entities;
using SecureCompany.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using SecureCompany.Application.Requirements;

namespace SecureCompany.Application.Handlers
{
    public class ApproveLeaveRequestHandler
    : AuthorizationHandler<ApproveLeaveRequestRequirement, LeaveRequest>
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public ApproveLeaveRequestHandler(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ApproveLeaveRequestRequirement requirement,
        LeaveRequest resource)
        {
            var currentUserId = context.User.FindFirstValue("sub");
            if (currentUserId is null)
                return;

            // if the current user is the one who created the leave request, they cannot approve it
            if (resource.UserId == currentUserId)
                return;

            // Admin users can approve any leave request
            if (context.User.IsInRole("Admin"))
            {
                context.Succeed(requirement);
                return;
            }

            // HR can approve requests from their department.
            if (!context.User.IsInRole("HR"))
                return;

            var currentUser = await _userManager.FindByIdAsync(currentUserId);
            var requestOwner = await _userManager.FindByIdAsync(resource.UserId);

            if (currentUser?.Department is null || requestOwner?.Department is null)
                return;

            if (string.Equals(
                currentUser.Department,
                requestOwner.Department,
                StringComparison.OrdinalIgnoreCase
            ))
            {
                context.Succeed(requirement);
            }
        }
    }
}