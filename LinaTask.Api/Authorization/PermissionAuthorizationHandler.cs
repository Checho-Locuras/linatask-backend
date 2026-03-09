using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace LinaTask.Api.Authorization
{
    /// <summary>
    /// Handler que evalúa si el usuario tiene el permiso requerido
    /// </summary>
    public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PermissionRequirement requirement)
        {
            // Buscar si el usuario tiene el claim de permiso requerido
            var hasPermission = context.User.Claims
                .Any(c => c.Type == "permission" && c.Value == requirement.Permission);

            if (hasPermission)
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}