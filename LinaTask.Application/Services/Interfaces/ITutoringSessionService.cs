// LinaTask.Application/Services/Interfaces/ITutoringSessionService.cs

using LinaTask.Application.DTOs;
using LinaTask.Domain.Enums;

namespace LinaTask.Application.Services.Interfaces
{
    public interface ITutoringSessionService
    {
        // ── Lecturas ─────────────────────────────────────────────
        Task<IEnumerable<TutoringSessionDto>> GetAllSessionsAsync();
        Task<IEnumerable<TutoringSessionDto>> GetSessionsByStudentAsync(Guid studentId);
        Task<IEnumerable<TutoringSessionDto>> GetSessionsByTeacherAsync(Guid teacherId);
        Task<IEnumerable<TutoringSessionDto>> GetSessionsByStatusAsync(SessionStatus status);
        Task<IEnumerable<TutoringSessionDto>> GetUpcomingSessionsAsync(Guid? userId = null);

        // requestingUserId permite al servicio inyectar el token de video correcto
        Task<TutoringSessionDto?> GetSessionByIdAsync(Guid id, Guid? requestingUserId = null);

        // ── Escritura ─────────────────────────────────────────────
        Task<TutoringSessionDto> CreateSessionAsync(CreateTutoringSessionDto createDto);
        Task<TutoringSessionDto> UpdateSessionAsync(Guid id, UpdateTutoringSessionDto updateDto);
        Task<bool> DeleteSessionAsync(Guid id);

        // ── Stats ─────────────────────────────────────────────────
        Task<SessionStatsDto> GetSessionStatsAsync(Guid? userId = null);

        // ── Video ─────────────────────────────────────────────────
        Task<VideoRoomAccessDto> GetOrCreateVideoRoomAsync(Guid sessionId, Guid requestingUserId);

        // ── Rating ────────────────────────────────────────────────
        Task<SessionRatingDto> CreateRatingAsync(CreateSessionRatingDto dto, Guid ratedByUserId);

    }
}