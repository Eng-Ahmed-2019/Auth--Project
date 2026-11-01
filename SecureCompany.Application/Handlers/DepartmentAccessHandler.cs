using Microsoft.AspNetCore.Authorization;
using SecureCompany.Application.Requirements;

namespace SecureCompany.Application.Handlers
{
    public class DepartmentAccessHandler
        : AuthorizationHandler<DepartmentAccessRequirement, string>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            DepartmentAccessRequirement requirement,
            string resourceDepartment)
        {
            string? userDepartment =
                context.User.FindFirst("Department")?.Value;

            if (string.IsNullOrWhiteSpace(userDepartment))
                return Task.CompletedTask;

            if (string.Equals(
                userDepartment.Trim(),
                resourceDepartment.Trim(),
                StringComparison.OrdinalIgnoreCase))
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}