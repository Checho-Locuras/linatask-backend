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

        // Método auxiliar para cargar las relaciones comunes de User
        private IQueryable<User> GetUserWithIncludes()
        {
            return _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(u => u.TeacherProfile)
                .Include(u => u.AcademicProfiles)
                    .ThenInclude(ap => ap.Institution)
                        .ThenInclude(i => i.City)
                            .ThenInclude(c => c.Department)
                                .ThenInclude(d => d.Country)
                .Include(u => u.Addresses)
                    .ThenInclude(a => a.City)
                        .ThenInclude(c => c.Department)
                            .ThenInclude(d => d.Country);
        }

        // Método auxiliar para cargar relaciones básicas (sin todas las anidaciones)
        private IQueryable<User> GetUserWithBasicIncludes()
        {
            return _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(u => u.TeacherProfile)
                .Include(u => u.AcademicProfiles)
                    .ThenInclude(ap => ap.Institution)
                .Include(u => u.Addresses)
                    .ThenInclude(a => a.City);
        }

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            try
            {
                return await GetUserWithIncludes()
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                // Considera usar un logger aquí
                // _logger.LogError(ex, "Error al obtener todos los usuarios");
                throw new ApplicationException($"Error al obtener todos los usuarios: {ex.Message}", ex);
            }
        }

        public async Task<User?> GetByIdAsync(Guid id)
        {
            return await GetUserWithIncludes()
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                    throw new ArgumentException("El email no puede estar vacío", nameof(email));

                return await GetUserWithBasicIncludes()
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

            }catch(Exception e)
            {

            }
            return null;
        }

        public async Task<User?> GetByPhoneAsync(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                throw new ArgumentException("El teléfono no puede estar vacío", nameof(phone));

            return await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(u => u.TeacherProfile)
                .FirstOrDefaultAsync(u => u.PhoneNumber == phone);
        }

        public async Task<User> CreateAsync(User user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            try
            {
                user.CreatedAt = DateTime.UtcNow;

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Recargar el usuario con todas las relaciones
                return await GetByIdAsync(user.Id) ?? user;
            }
            catch (Exception ex)
            {
                // Considera usar un logger aquí
                // _logger.LogError(ex, "Error al crear usuario");
                throw new ApplicationException($"Error al crear el usuario: {ex.Message}", ex);
            }
        }

        public async Task<User> UpdateAsync(User user)
        {
            // DEBUG TEMPORAL
            foreach (var entry in _context.ChangeTracker.Entries())
            {
                if (entry.State != EntityState.Unchanged && entry.State != EntityState.Detached)
                {
                    Console.WriteLine($"[TRACKER] {entry.Entity.GetType().Name} | State: {entry.State}");
                    foreach (var prop in entry.Properties)
                    {
                        if (prop.IsModified || entry.State == EntityState.Added)
                            Console.WriteLine($"  {prop.Metadata.Name}: Original=[{prop.OriginalValue}] Current=[{prop.CurrentValue}]");
                    }
                }
            }
            // FIN DEBUG

            try
            {
                await _context.SaveChangesAsync();
                return await GetByIdAsync(user.Id) ?? user;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                throw new ApplicationException("El usuario fue modificado o eliminado por otro proceso.", ex);
            }
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return false;

            try
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                // Considera usar un logger aquí
                // _logger.LogError(ex, "Error al eliminar usuario");
                throw new ApplicationException($"Error al eliminar el usuario: {ex.Message}", ex);
            }
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.Users.AnyAsync(u => u.Id == id);
        }

        public async Task<bool> ExistsByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("El email no puede estar vacío", nameof(email));

            return await _context.Users
                .AnyAsync(u => u.Email.ToLower() == email.ToLower());
        }

        public async Task<UserAddress> AddAddressAsync(UserAddress address)
        {
            if (address == null)
                throw new ArgumentNullException(nameof(address));

            try
            {
                address.CreatedAt = DateTime.UtcNow;
                _context.UserAddresses.Add(address);
                await _context.SaveChangesAsync();
                return address;
            }
            catch (Exception ex)
            {
                // Considera usar un logger aquí
                // _logger.LogError(ex, "Error al agregar dirección");
                throw new ApplicationException($"Error al agregar la dirección: {ex.Message}", ex);
            }
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
            if (address == null)
                throw new ArgumentNullException(nameof(address));

            try
            {
                _context.UserAddresses.Update(address);
                await _context.SaveChangesAsync();
                return address;
            }
            catch (Exception ex)
            {
                // Considera usar un logger aquí
                // _logger.LogError(ex, "Error al actualizar dirección");
                throw new ApplicationException($"Error al actualizar la dirección: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteAddressAsync(Guid addressId)
        {
            var address = await _context.UserAddresses.FindAsync(addressId);
            if (address == null)
                return false;

            try
            {
                _context.UserAddresses.Remove(address);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                // Considera usar un logger aquí
                // _logger.LogError(ex, "Error al eliminar dirección");
                throw new ApplicationException($"Error al eliminar la dirección: {ex.Message}", ex);
            }
        }

        // Métodos adicionales para manejar roles
        public async Task<IEnumerable<UserRole>> GetUserRolesAsync(Guid userId)
        {
            return await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .Include(ur => ur.Role)
                .ToListAsync();
        }

        public async Task<bool> HasRoleAsync(Guid userId, string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
                throw new ArgumentException("El nombre del rol no puede estar vacío", nameof(roleName));

            return await _context.UserRoles
                .Include(ur => ur.Role)
                .AnyAsync(ur => ur.UserId == userId && ur.Role.Name.ToLower() == roleName.ToLower());
        }

        public async Task DeleteUserRolesAsync(Guid userId)
        {
            var roles = await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .ToListAsync();

            _context.UserRoles.RemoveRange(roles);
        }

        public async Task SyncAcademicProfilesAsync(Guid userId, List<UserAcademicProfile> profiles)
        {
            // 1. Traer los perfiles actuales de la BD
            var existing = await _context.UserAcademicProfiles
                .Where(p => p.UserId == userId)
                .ToListAsync();

            var incomingIds = profiles
                .Where(p => p.Id != Guid.Empty)
                .Select(p => p.Id)
                .ToHashSet();

            // 2. Eliminar los que ya no vienen
            var toDelete = existing
                .Where(p => !incomingIds.Contains(p.Id))
                .ToList();

            if (toDelete.Any())
                _context.UserAcademicProfiles.RemoveRange(toDelete);

            // 3. Insertar o actualizar
            foreach (var profile in profiles)
            {
                var existingProfile = existing.FirstOrDefault(p => p.Id == profile.Id);

                if (existingProfile != null)
                {
                    // Actualizar el existente
                    existingProfile.RoleId = profile.RoleId;
                    existingProfile.InstitutionId = profile.InstitutionId;
                    existingProfile.EducationLevel = profile.EducationLevel;
                    existingProfile.CurrentSemester = profile.CurrentSemester;
                    existingProfile.CurrentGrade = profile.CurrentGrade;
                    existingProfile.GraduationYear = profile.GraduationYear;
                    existingProfile.StudyArea = profile.StudyArea;
                    existingProfile.AcademicStatus = profile.AcademicStatus;
                    existingProfile.ProfessionalDescription = profile.ProfessionalDescription;
                }
                else
                {
                    // Insertar nuevo directamente al DbSet
                    _context.UserAcademicProfiles.Add(new UserAcademicProfile
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        RoleId = profile.RoleId,
                        InstitutionId = profile.InstitutionId,
                        EducationLevel = profile.EducationLevel,
                        CurrentSemester = profile.CurrentSemester,
                        CurrentGrade = profile.CurrentGrade,
                        GraduationYear = profile.GraduationYear,
                        StudyArea = profile.StudyArea,
                        AcademicStatus = profile.AcademicStatus,
                        ProfessionalDescription = profile.ProfessionalDescription,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
        }
    }
}