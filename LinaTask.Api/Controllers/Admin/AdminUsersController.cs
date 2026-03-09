using LinaTask.Api.Attributes;
using LinaTask.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinaTask.API.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/users")]
    [Authorize]
    public class AdminUsersController : ControllerBase
    {
        private readonly IAdminUserService _adminUserService;

        public AdminUsersController(IAdminUserService adminUserService)
        {
            _adminUserService = adminUserService;
        }

        [HttpPost("{userId}/make-admin")]
        [PermissionAuthorize("ADMIN.PROMOTE_USER")]
        public async Task<IActionResult> MakeAdmin(Guid userId)
        {
            await _adminUserService.PromoteToAdminAsync(userId);
            return Ok(new { message = "Usuario promovido a ADMIN" });
        }

        [HttpPost("{userId}/remove-admin")]
        [PermissionAuthorize("ADMIN.DEMOTE_USER")]
        public async Task<IActionResult> RemoveAdmin(Guid userId)
        {
            await _adminUserService.RemoveAdminAsync(userId);
            return Ok(new { message = "Rol ADMIN removido" });
        }
    }
}