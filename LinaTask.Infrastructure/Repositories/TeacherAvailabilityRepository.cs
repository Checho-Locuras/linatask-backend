using LinaTask.Domain.Interfaces;
using LinaTask.Domain.Models;
using LinaTask.Infrastructure.DataBaseContext;
using Microsoft.EntityFrameworkCore;

namespace LinaTask.Infrastructure.Repositories
{
    public class TeacherAvailabilityRepository : ITeacherAvailabilityRepository
    {
        private readonly LinaTaskDbContext _context;

        public TeacherAvailabilityRepository(LinaTaskDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<TeacherAvailability>> GetByTeacherIdAsync(Guid teacherId)
        {
            return await _context.TeacherAvailabilities
                .Include(a => a.Teacher)
                .Where(a => a.TeacherId == teacherId)
                .OrderBy(a => a.DayOfWeek)
                .ThenBy(a => a.StartTime)
                .ToListAsync();
        }

        public async Task<TeacherAvailability?> GetByIdAsync(Guid id)
        {
            return await _context.TeacherAvailabilities
                .Include(a => a.Teacher)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<TeacherAvailability> CreateAsync(TeacherAvailability availability)
        {
            availability.Id = Guid.NewGuid();
            availability.CreatedAt = DateTime.UtcNow;
            availability.UpdatedAt = DateTime.UtcNow;
            _context.TeacherAvailabilities.Add(availability);
            await _context.SaveChangesAsync();
            return availability;
        }

        public async Task<TeacherAvailability> UpdateAsync(TeacherAvailability availability)
        {
            availability.UpdatedAt = DateTime.UtcNow;
            _context.TeacherAvailabilities.Update(availability);
            await _context.SaveChangesAsync();
            return availability;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var entity = await _context.TeacherAvailabilities.FindAsync(id);
            if (entity == null) return false;
            _context.TeacherAvailabilities.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task DeleteByTeacherIdAsync(Guid teacherId)
        {
            var entities = await _context.TeacherAvailabilities
                .Where(a => a.TeacherId == teacherId)
                .ToListAsync();
            _context.TeacherAvailabilities.RemoveRange(entities);
            await _context.SaveChangesAsync();
        }

        public async Task BulkInsertAsync(IEnumerable<TeacherAvailability> availabilities)
        {
            await _context.TeacherAvailabilities.AddRangeAsync(availabilities);
            await _context.SaveChangesAsync();
        }
    }
}