using System.Security.Claims;
using ecureCompany.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using SecureCompany.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using SecureCompany.Application.Requirements;

namespace SecureCompany.Application.Handlers
{
    public class LeaveRequestAccessHandler :
        AuthorizationHandler<LeaveRequestAccessRequirement, LeaveRequest>
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public LeaveRequestAccessHandler(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            LeaveRequestAccessRequirement requirement,
            LeaveRequest resource)
        {
            var userId = context.User.FindFirstValue("sub");
            if (string.IsNullOrWhiteSpace(userId))
                return;

            // Admins have access to all leave requests
            if (context.User.IsInRole("Admin"))
            {
                context.Succeed(requirement);
                return;
            }

            // Employees can access their own leave requests
            if (resource.UserId == userId)
            {
                context.Succeed(requirement);
                return;
            }

            // HR can access leave requests of employees in their department
            if (context.User.IsInRole("HR"))
            {
                var currentUser = await _userManager.FindByIdAsync(userId);
                if (currentUser?.Department is not null)
                {
                    var owner = await _userManager.FindByIdAsync(resource.UserId);
                    if (owner?.Department is not null &&
                        string.Equals(
                            currentUser.Department,
                            owner.Department,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    {
                        context.Succeed(requirement);
                    }
                }
            }
        }
    }
}