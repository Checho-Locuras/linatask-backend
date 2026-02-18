using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LinaTask.Api.Common
{
    [ApiController]
    public abstract class BaseController : ControllerBase
    {
        protected Guid CurrentUserId
        {
            get
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                    throw new UnauthorizedAccessException("Usuario no autenticado");

                return Guid.Parse(userId);
            }
        }

        protected string CurrentUserName =>
            User.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;

        protected string CurrentUserEmail =>
            User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;

    }
}
