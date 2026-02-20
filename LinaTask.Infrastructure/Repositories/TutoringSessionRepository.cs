using LinaTask.Domain.Enums;
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

        private IQueryable<TutoringSession> BaseQuery() =>
            _context.TutoringSessions
                .Include(ts => ts.Student)
                .Include(ts => ts.Teacher)
                .Include(ts => ts.Subject)
                .Include(ts => ts.Rating);

        public async Task<IEnumerable<TutoringSession>> GetAllAsync() =>
            await BaseQuery().OrderByDescending(ts => ts.StartTime).ToListAsync();

        public async Task<IEnumerable<TutoringSession>> GetByStudentIdAsync(Guid studentId) =>
            await BaseQuery()
                .Where(ts => ts.StudentId == studentId)
                .OrderByDescending(ts => ts.StartTime)
                .ToListAsync();

        public async Task<IEnumerable<TutoringSession>> GetByTeacherIdAsync(Guid teacherId) =>
            await BaseQuery()
                .Where(ts => ts.TeacherId == teacherId)
                .OrderByDescending(ts => ts.StartTime)
                .ToListAsync();

        public async Task<IEnumerable<TutoringSession>> GetByStatusAsync(SessionStatus status) =>
            await BaseQuery()
                .Where(ts => ts.Status == status)
                .OrderBy(ts => ts.StartTime)
                .ToListAsync();

        public async Task<IEnumerable<TutoringSession>> GetUpcomingSessionsAsync(Guid? userId = null)
        {
            var query = BaseQuery()
                .Where(ts => ts.StartTime > DateTime.UtcNow &&
                             (ts.Status == SessionStatus.Scheduled || ts.Status == SessionStatus.Ready));

            if (userId.HasValue)
                query = query.Where(ts => ts.StudentId == userId || ts.TeacherId == userId);

            return await query.OrderBy(ts => ts.StartTime).ToListAsync();
        }

        public async Task<TutoringSession?> GetByIdAsync(Guid id) =>
            await BaseQuery().FirstOrDefaultAsync(ts => ts.Id == id);

        public async Task<TutoringSession> CreateAsync(TutoringSession session)
        {
            _context.TutoringSessions.Add(session);
            await _context.SaveChangesAsync();
            return (await GetByIdAsync(session.Id))!;
        }

        public async Task<TutoringSession> UpdateAsync(TutoringSession session)
        {
            session.UpdatedAt = DateTime.UtcNow;
            _context.TutoringSessions.Update(session);
            await _context.SaveChangesAsync();
            return (await GetByIdAsync(session.Id))!;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var session = await _context.TutoringSessions.FindAsync(id);
            if (session is null) return false;
            _context.TutoringSessions.Remove(session);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<SessionRating> CreateRatingAsync(SessionRating rating)
        {
            _context.SessionRatings.Add(rating);
            await _context.SaveChangesAsync();
            return rating;
        }

        public async Task<SessionRating?> GetRatingBySessionIdAsync(Guid sessionId) =>
            await _context.SessionRatings.FirstOrDefaultAsync(r => r.SessionId == sessionId);

        public async Task<int> GetSessionCountByTeacherAsync(Guid teacherId, DateTime? startDate = null)
        {
            var query = _context.TutoringSessions
                .Where(ts => ts.TeacherId == teacherId && ts.Status == SessionStatus.Completed);

            if (startDate.HasValue)
                query = query.Where(ts => ts.StartTime >= startDate.Value);

            return await query.CountAsync();
        }
    }
}