using LinaTask.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace LinaTask.Application.Services.Interfaces
{
    public interface ITutoringSessionService
    {
        Task<IEnumerable<TutoringSessionDto>> GetAllSessionsAsync();
        Task<IEnumerable<TutoringSessionDto>> GetSessionsByStudentAsync(Guid studentId);
        Task<IEnumerable<TutoringSessionDto>> GetSessionsByTeacherAsync(Guid teacherId);
        Task<IEnumerable<TutoringSessionDto>> GetSessionsByStatusAsync(string status);
        Task<IEnumerable<TutoringSessionDto>> GetUpcomingSessionsAsync(Guid? userId = null);
        Task<TutoringSessionDto?> GetSessionByIdAsync(Guid id);
        Task<TutoringSessionDto> CreateSessionAsync(CreateTutoringSessionDto createDto);
        Task<TutoringSessionDto> UpdateSessionAsync(Guid id, UpdateTutoringSessionDto updateDto);
        Task<bool> DeleteSessionAsync(Guid id);
        Task<SessionStatsDto> GetSessionStatsAsync(Guid? userId = null);
    }
}
