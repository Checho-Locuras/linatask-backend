using LinaTask.Domain.DTOs;

namespace LinaTask.Application.Services.Interfaces
{
    public interface IUserService
    {
        Task<UserDto> CreateUserAsync(CreateUserDto createUserDto);
        Task<bool> DeleteUserAsync(Guid id);
        Task<IEnumerable<UserDto>> GetAllUsersAsync();
        Task<UserDto?> GetUserByEmailAsync(string email);
        Task<UserDto?> GetUserByIdAsync(Guid id);
        Task<UserDto> UpdateUserAsync(Guid id, UpdateUserDto updateUserDto);
        Task<UserAddressDto> AddAddressAsync(Guid userId, CreateAddressDto createAddressDto);
        Task<UserAddressDto> UpdateAddressAsync(Guid addressId, UpdateAddressDto updateAddressDto);
        Task<bool> DeleteAddressAsync(Guid addressId);
        Task<UserAddressDto> SetPrimaryAddressAsync(Guid addressId);
        Task<IEnumerable<UserAddressDto>> GetUserAddressesAsync(Guid userId);
    }
}