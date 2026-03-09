using LinaTask.Application.Services.Interfaces;
using LinaTask.Domain.Interfaces;
using System;
using System.Threading.Tasks;

namespace LinaTask.Application.Services
{
    public class AdminUserService : IAdminUserService
    {
        private readonly IUserRoleRepository _userRoleRepository;

        public AdminUserService(IUserRoleRepository userRoleRepository)
        {
            _userRoleRepository = userRoleRepository;
        }

        public async Task PromoteToAdminAsync(Guid userId)
        {
            var isAlreadyAdmin = await _userRoleRepository.UserHasRoleAsync(userId, "ADMIN");
            if (isAlreadyAdmin)
                return;

            await _userRoleRepository.AssignRoleAsync(userId, "ADMIN");
        }

        public async Task RemoveAdminAsync(Guid userId)
        {
            await _userRoleRepository.RemoveRoleAsync(userId, "ADMIN");
        }
    }
}
