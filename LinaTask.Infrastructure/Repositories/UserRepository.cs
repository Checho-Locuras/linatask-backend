using LinaTask.Domain.Interfaces;
using LinaTask.Domain.Models;
using LinaTask.Infrastructure.DataBaseContext;
using Microsoft.EntityFrameworkCore;

namespace LinaTask.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly LinaTaskDbContext _context;

        public UserRepository(LinaTaskDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            try
            {
                return await _context.Users
                    .Include(u => u.TeacherProfile)
                    .Include(u => u.AcademicProfiles)
                        .ThenInclude(ap => ap.Institution)
                            .ThenInclude(i => i.City)
                                .ThenInclude(c => c.Department)
                                    .ThenInclude(d => d.Country)
                    .Include(u => u.Addresses)
                        .ThenInclude(a => a.City)
                            .ThenInclude(c => c.Department)
                                .ThenInclude(d => d.Country)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception e)
            {
                // Log the exception
                throw; // Es mejor propagar la excepción que devolver null
            }
        }

        public async Task<User?> GetByIdAsync(Guid id)
        {
            return await _context.Users
                .Include(u => u.TeacherProfile)
                .Include(u => u.AcademicProfiles)
                    .ThenInclude(ap => ap.Institution)
                        .ThenInclude(i => i.City)
                            .ThenInclude(c => c.Department)
                                .ThenInclude(d => d.Country)
                .Include(u => u.Addresses)
                    .ThenInclude(a => a.City)
                        .ThenInclude(c => c.Department)
                            .ThenInclude(d => d.Country)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users
                .Include(u => u.TeacherProfile)
                .Include(u => u.AcademicProfiles)
                    .ThenInclude(ap => ap.Institution)
                .Include(u => u.Addresses)
                    .ThenInclude(a => a.City)
                .FirstOrDefaultAsync(u => u.Email == email.ToLower());
        }

        public async Task<User?> GetByPhoneAsync(string phone)
        {
            return await _context.Users
                .Include(u => u.TeacherProfile)
                .FirstOrDefaultAsync(u => u.PhoneNumber == phone);
        }

        public async Task<User> CreateAsync(User user)
        {
            try {
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }catch (Exception ex)
            {

            }
            // Recargar el usuario con todas las relaciones
            return await GetByIdAsync(user.Id) ?? user;
        }

        public async Task<User> UpdateAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return false;

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.Users.AnyAsync(u => u.Id == id);
        }

        public async Task<UserAddress> AddAddressAsync(UserAddress address)
        {
            _context.UserAddresses.Add(address);
            await _context.SaveChangesAsync();
            return address;
        }

        public async Task<UserAddress?> GetAddressByIdAsync(Guid addressId)
        {
            return await _context.UserAddresses
                .Include(a => a.City)
                    .ThenInclude(c => c.Department)
                        .ThenInclude(d => d.Country)
                .FirstOrDefaultAsync(a => a.Id == addressId);
        }

        public async Task<IEnumerable<UserAddress>> GetUserAddressesAsync(Guid userId)
        {
            return await _context.UserAddresses
                .Where(a => a.UserId == userId)
                .Include(a => a.City)
                    .ThenInclude(c => c.Department)
                        .ThenInclude(d => d.Country)
                .OrderByDescending(a => a.IsPrimary)
                .ThenByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<UserAddress> UpdateAddressAsync(UserAddress address)
        {
            _context.UserAddresses.Update(address);
            await _context.SaveChangesAsync();
            return address;
        }

        public async Task<bool> DeleteAddressAsync(Guid addressId)
        {
            var address = await _context.UserAddresses.FindAsync(addressId);
            if (address == null)
                return false;

            _context.UserAddresses.Remove(address);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}