using LinaTask.Domain.Models;

namespace LinaTask.Infrastructure.Repositories
{
    public interface IUserRepository
    {
        Task<UserAddress> AddAddressAsync(UserAddress address);
        Task<User> CreateAsync(User user);
        Task<bool> DeleteAddressAsync(Guid addressId);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
        Task<bool> ExistsByEmailAsync(string email);
        Task<UserAddress?> GetAddressByIdAsync(Guid addressId);
        Task<IEnumerable<User>> GetAllAsync();
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByIdAsync(Guid id);
        Task<User?> GetByPhoneAsync(string phone);
        Task<IEnumerable<UserAddress>> GetUserAddressesAsync(Guid userId);
        Task<IEnumerable<UserRole>> GetUserRolesAsync(Guid userId);
        Task<bool> HasRoleAsync(Guid userId, string roleName);
        Task<UserAddress> UpdateAddressAsync(UserAddress address);
        Task<User> UpdateAsync(User user);
        Task DeleteUserRolesAsync(Guid userId);
        Task SyncAcademicProfilesAsync(Guid userId, List<UserAcademicProfile> profiles);
    }
}