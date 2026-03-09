using Microsoft.AspNetCore.Authorization;

namespace LinaTask.Api.Authorization
{
    /// <summary>
    /// Requirement para verificar permisos
    /// </summary>
    public class PermissionRequirement : IAuthorizationRequirement
    {
        public string Permission { get; }

        public PermissionRequirement(string permission)
        {
            Permission = permission;
        }
    }
}