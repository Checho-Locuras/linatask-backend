using LinaTask.Domain.Interfaces;
using LinaTask.Domain.Models;
using LinaTask.Infrastructure.DataBaseContext;
using Microsoft.EntityFrameworkCore;

namespace LinaTask.Infrastructure.Repositories
{
    public class TutoringSessionRepository : ITutoringSessionRepository
    {
        private readonly LinaTaskDbContext _context;

        public TutoringSessionRepository(LinaTaskDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<TutoringSession>> GetAllAsync()
        {
            return await _context.TutoringSessions
                .Include(ts => ts.Student)
                .Include(ts => ts.Teacher)
                .OrderBy(ts => ts.SessionDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<TutoringSession>> GetByStudentIdAsync(Guid studentId)
        {
            return await _context.TutoringSessions
                .Include(ts => ts.Teacher)
                .Where(ts => ts.StudentId == studentId)
                .OrderBy(ts => ts.SessionDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<TutoringSession>> GetByTeacherIdAsync(Guid teacherId)
        {
            return await _context.TutoringSessions
                .Include(ts => ts.Student)
                .Where(ts => ts.TeacherId == teacherId)
                .OrderBy(ts => ts.SessionDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<TutoringSession>> GetByStatusAsync(string status)
        {
            return await _context.TutoringSessions
                .Include(ts => ts.Student)
                .Include(ts => ts.Teacher)
                .Where(ts => ts.Status == status)
                .OrderBy(ts => ts.SessionDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<TutoringSession>> GetUpcomingSessionsAsync(Guid? userId = null)
        {
            var query = _context.TutoringSessions
                .Include(ts => ts.Student)
                .Include(ts => ts.Teacher)
                .Where(ts => ts.SessionDate >= DateTime.UtcNow && ts.Status == "scheduled");

            if (userId.HasValue)
            {
                query = query.Where(ts => ts.StudentId == userId || ts.TeacherId == userId);
            }

            return await query
                .OrderBy(ts => ts.SessionDate)
                .ToListAsync();
        }

        public async Task<TutoringSession?> GetByIdAsync(Guid id)
        {
            return await _context.TutoringSessions
                .Include(ts => ts.Student)
                .Include(ts => ts.Teacher)
                .FirstOrDefaultAsync(ts => ts.Id == id);
        }

        public async Task<TutoringSession> CreateAsync(TutoringSession session)
        {
            _context.TutoringSessions.Add(session);
            await _context.SaveChangesAsync();
            return session;
        }

        public async Task<TutoringSession> UpdateAsync(TutoringSession session)
        {
            _context.TutoringSessions.Update(session);
            await _context.SaveChangesAsync();
            return session;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var session = await GetByIdAsync(id);
            if (session == null) return false;

            _context.TutoringSessions.Remove(session);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetSessionCountByTeacherAsync(Guid teacherId, DateTime? startDate = null)
        {
            var query = _context.TutoringSessions
                .Where(ts => ts.TeacherId == teacherId && ts.Status == "completed");

            if (startDate.HasValue)
                query = query.Where(ts => ts.SessionDate >= startDate.Value);

            return await query.CountAsync();
        }
    }
}