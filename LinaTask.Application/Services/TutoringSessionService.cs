using LinaTask.Application.DTOs;
using LinaTask.Application.Services.Interfaces;
using LinaTask.Domain.Interfaces;
using LinaTask.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace LinaTask.Application.Services
{
    public class TutoringSessionService : ITutoringSessionService
    {
        private readonly ITutoringSessionRepository _sessionRepository;
        private readonly IUserRepository _userRepository;

        public TutoringSessionService(
            ITutoringSessionRepository sessionRepository,
            IUserRepository userRepository)
        {
            _sessionRepository = sessionRepository;
            _userRepository = userRepository;
        }

        public async Task<IEnumerable<TutoringSessionDto>> GetAllSessionsAsync()
        {
            var sessions = await _sessionRepository.GetAllAsync();
            return sessions.Select(MapToDto);
        }

        public async Task<IEnumerable<TutoringSessionDto>> GetSessionsByStudentAsync(Guid studentId)
        {
            var sessions = await _sessionRepository.GetByStudentIdAsync(studentId);
            return sessions.Select(MapToDto);
        }

        public async Task<IEnumerable<TutoringSessionDto>> GetSessionsByTeacherAsync(Guid teacherId)
        {
            var sessions = await _sessionRepository.GetByTeacherIdAsync(teacherId);
            return sessions.Select(MapToDto);
        }

        public async Task<IEnumerable<TutoringSessionDto>> GetSessionsByStatusAsync(string status)
        {
            var sessions = await _sessionRepository.GetByStatusAsync(status);
            return sessions.Select(MapToDto);
        }

        public async Task<IEnumerable<TutoringSessionDto>> GetUpcomingSessionsAsync(Guid? userId = null)
        {
            var sessions = await _sessionRepository.GetUpcomingSessionsAsync(userId);
            return sessions.Select(MapToDto);
        }

        public async Task<TutoringSessionDto?> GetSessionByIdAsync(Guid id)
        {
            var session = await _sessionRepository.GetByIdAsync(id);
            return session == null ? null : MapToDto(session);
        }

        public async Task<TutoringSessionDto> CreateSessionAsync(CreateTutoringSessionDto createDto)
        {
            // Validar que el estudiante existe
            var student = await _userRepository.GetByIdAsync(createDto.StudentId);
            if (student == null || student.Role != "student")
                throw new InvalidOperationException("Invalid student ID");

            // Validar que el profesor existe
            var teacher = await _userRepository.GetByIdAsync(createDto.TeacherId);
            if (teacher == null || teacher.Role != "teacher")
                throw new InvalidOperationException("Invalid teacher ID");

            // Validar que la fecha de la sesión sea futura
            if (createDto.SessionDate <= DateTime.UtcNow)
                throw new InvalidOperationException("Session date must be in the future");

            var session = new TutoringSession
            {
                Id = Guid.NewGuid(),
                StudentId = createDto.StudentId,
                TeacherId = createDto.TeacherId,
                SessionDate = createDto.SessionDate,
                MeetLink = createDto.MeetLink!,
                Status = "scheduled",
                CreatedAt = DateTime.UtcNow
            };

            var createdSession = await _sessionRepository.CreateAsync(session);
            return MapToDto(createdSession);
        }

        public async Task<TutoringSessionDto> UpdateSessionAsync(Guid id, UpdateTutoringSessionDto updateDto)
        {
            var session = await _sessionRepository.GetByIdAsync(id);
            if (session == null)
                throw new KeyNotFoundException($"Session with ID {id} not found");

            if (updateDto.SessionDate.HasValue)
            {
                // Solo permitir cambiar la fecha si la sesión está programada
                if (session.Status != "scheduled")
                    throw new InvalidOperationException("Can only change date for scheduled sessions");
                if (updateDto.SessionDate.Value <= DateTime.UtcNow)
                    throw new InvalidOperationException("Session date must be in the future");

                session.SessionDate = updateDto.SessionDate.Value;
            }

            if (updateDto.MeetLink != null)
                session.MeetLink = updateDto.MeetLink;

            if (!string.IsNullOrEmpty(updateDto.Status))
            {
                // Validar transiciones de estado
                var validTransitions = new Dictionary<string, string[]>
            {
                { "scheduled", new[] { "completed", "cancelled", "no_show" } },
                { "completed", Array.Empty<string>() },
                { "cancelled", new[] { "scheduled" } },
                { "no_show", Array.Empty<string>() }
            };

                if (validTransitions.ContainsKey(session.Status))
                {
                    if (!validTransitions[session.Status].Contains(updateDto.Status))
                        throw new InvalidOperationException(
                            $"Invalid status transition from {session.Status} to {updateDto.Status}");
                }

                session.Status = updateDto.Status;
            }

            session.CreatedAt = session.CreatedAt.ToUniversalTime();

            var updatedSession = await _sessionRepository.UpdateAsync(session);
            return MapToDto(updatedSession);
        }

        public async Task<bool> DeleteSessionAsync(Guid id)
        {
            return await _sessionRepository.DeleteAsync(id);
        }

        public async Task<SessionStatsDto> GetSessionStatsAsync(Guid? userId = null)
        {
            IEnumerable<TutoringSession> sessions;

            if (userId.HasValue)
            {
                var studentSessions = await _sessionRepository.GetByStudentIdAsync(userId.Value);
                var teacherSessions = await _sessionRepository.GetByTeacherIdAsync(userId.Value);
                sessions = studentSessions.Concat(teacherSessions).Distinct();
            }
            else
            {
                sessions = await _sessionRepository.GetAllAsync();
            }

            var sessionsList = sessions.ToList();

            return new SessionStatsDto
            {
                TotalSessions = sessionsList.Count,
                ScheduledSessions = sessionsList.Count(s => s.Status == "scheduled"),
                CompletedSessions = sessionsList.Count(s => s.Status == "completed"),
                CancelledSessions = sessionsList.Count(s => s.Status == "cancelled"),
                NoShowSessions = sessionsList.Count(s => s.Status == "no_show")
            };
        }

        private static TutoringSessionDto MapToDto(TutoringSession session)
        {
            return new TutoringSessionDto
            {
                Id = session.Id,
                StudentId = session.StudentId,
                StudentName = session.Student?.Name ?? string.Empty,
                TeacherId = session.TeacherId,
                TeacherName = session.Teacher?.Name ?? string.Empty,
                SessionDate = session.SessionDate,
                MeetLink = session.MeetLink,
                Status = session.Status,
                CreatedAt = session.CreatedAt
            };
        }
    }
}
