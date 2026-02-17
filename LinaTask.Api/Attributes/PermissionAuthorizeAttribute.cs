using Microsoft.AspNetCore.Authorization;

namespace LinaTask.Api.Attributes
{
    /// <summary>
    /// Atributo para autorización basada en permisos
    /// </summary>
    public class PermissionAuthorizeAttribute : AuthorizeAttribute
    {
        public PermissionAuthorizeAttribute(string permission)
        {
            Policy = $"Permission:{permission}";
        }
    }
}