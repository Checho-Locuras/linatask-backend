using LinaTask.Domain.Enums;
using LinaTask.Domain.Models;

namespace LinaTask.Domain.Interfaces
{
    public interface ITutoringSessionRepository
    {
        Task<IEnumerable<TutoringSession>> GetAllAsync();
        Task<IEnumerable<TutoringSession>> GetByStudentIdAsync(Guid studentId);
        Task<IEnumerable<TutoringSession>> GetByTeacherIdAsync(Guid teacherId);
        Task<IEnumerable<TutoringSession>> GetByStatusAsync(SessionStatus status);
        Task<IEnumerable<TutoringSession>> GetUpcomingSessionsAsync(Guid? userId = null);
        Task<TutoringSession?> GetByIdAsync(Guid id);
        Task<TutoringSession> CreateAsync(TutoringSession session);
        Task<TutoringSession> UpdateAsync(TutoringSession session);
        Task<bool> DeleteAsync(Guid id);

        // Ratings
        Task<SessionRating> CreateRatingAsync(SessionRating rating);
        Task<SessionRating?> GetRatingBySessionIdAsync(Guid sessionId);
    }
}