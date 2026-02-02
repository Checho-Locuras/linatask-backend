using LinaTask.Domain.Models;

namespace LinaTask.Domain.Interfaces
{
    public interface IUserRepository
    {
        Task<IEnumerable<User>> GetAllAsync();
        Task<User?> GetByIdAsync(Guid id);
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByPhoneAsync(string phone);
        Task<User> CreateAsync(User user);
        Task<User> UpdateAsync(User user);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
        Task<UserAddress> AddAddressAsync(UserAddress address);
        Task<UserAddress?> GetAddressByIdAsync(Guid addressId);
        Task<IEnumerable<UserAddress>> GetUserAddressesAsync(Guid userId);
        Task<UserAddress> UpdateAddressAsync(UserAddress address);
        Task<bool> DeleteAddressAsync(Guid addressId);
    }
}