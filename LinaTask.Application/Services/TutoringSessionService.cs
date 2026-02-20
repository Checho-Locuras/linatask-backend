// LinaTask.Application/Services/TutoringSessionService.cs
// Cambios: reemplaza ISessionNotificationService por INotificationService

using LinaTask.Application.DTOs;
using LinaTask.Application.Services.Interfaces;
using LinaTask.Domain.DTOs;
using LinaTask.Domain.Enums;
using LinaTask.Domain.Interfaces;
using LinaTask.Domain.Models;
using LinaTask.Infrastructure.Repositories;

namespace LinaTask.Application.Services
{
    public class TutoringSessionService : ITutoringSessionService
    {
        private readonly ITutoringSessionRepository _sessionRepository;
        private readonly IUserRepository _userRepository;
        private readonly IHmsVideoService _hmsService;
        private readonly INotificationService _notificationService;  // ← reemplaza _notifications

        public TutoringSessionService(
            ITutoringSessionRepository sessionRepository,
            IUserRepository userRepository,
            IHmsVideoService hmsService,
            INotificationService notificationService)
        {
            _sessionRepository = sessionRepository;
            _userRepository = userRepository;
            _hmsService = hmsService;
            _notificationService = notificationService;
        }

        // ─────────────────────────────────────────────────
        // LECTURAS  (sin cambios)
        // ─────────────────────────────────────────────────

        public async Task<IEnumerable<TutoringSessionDto>> GetAllSessionsAsync() =>
            (await _sessionRepository.GetAllAsync()).Select(MapToDto);

        public async Task<IEnumerable<TutoringSessionDto>> GetSessionsByStudentAsync(Guid studentId) =>
            (await _sessionRepository.GetByStudentIdAsync(studentId)).Select(MapToDto);

        public async Task<IEnumerable<TutoringSessionDto>> GetSessionsByTeacherAsync(Guid teacherId) =>
            (await _sessionRepository.GetByTeacherIdAsync(teacherId)).Select(MapToDto);

        public async Task<IEnumerable<TutoringSessionDto>> GetSessionsByStatusAsync(SessionStatus status) =>
            (await _sessionRepository.GetByStatusAsync(status)).Select(MapToDto);

        public async Task<IEnumerable<TutoringSessionDto>> GetUpcomingSessionsAsync(Guid? userId = null) =>
            (await _sessionRepository.GetUpcomingSessionsAsync(userId)).Select(MapToDto);

        public async Task<TutoringSessionDto?> GetSessionByIdAsync(Guid id, Guid? requestingUserId = null)
        {
            var session = await _sessionRepository.GetByIdAsync(id);
            if (session is null) return null;

            var dto = MapToDto(session);

            if (requestingUserId.HasValue)
            {
                if (requestingUserId == session.StudentId)
                    dto.VideoToken = session.StudentToken;
                else if (requestingUserId == session.TeacherId)
                    dto.VideoToken = session.TeacherToken;
            }

            return dto;
        }

        // ─────────────────────────────────────────────────
        // CREAR SESIÓN
        // ─────────────────────────────────────────────────

        public async Task<TutoringSessionDto> CreateSessionAsync(CreateTutoringSessionDto createDto)
        {
            var student = await _userRepository.GetByIdAsync(createDto.StudentId)
                ?? throw new InvalidOperationException("Student not found");

            if (!student.UserRoles.Any(ur => ur.Role.Name == "student"))
                throw new InvalidOperationException("User is not a student");

            var teacher = await _userRepository.GetByIdAsync(createDto.TeacherId)
                ?? throw new InvalidOperationException("Teacher not found");

            if (!teacher.UserRoles.Any(ur => ur.Role.Name == "teacher"))
                throw new InvalidOperationException("User is not a teacher");

            if (createDto.StartTime <= DateTime.UtcNow)
                throw new InvalidOperationException("StartTime must be in the future");

            if (createDto.EndTime <= createDto.StartTime)
                throw new InvalidOperationException("EndTime must be after StartTime");

            var session = new TutoringSession
            {
                Id = Guid.NewGuid(),
                StudentId = createDto.StudentId,
                TeacherId = createDto.TeacherId,
                SubjectId = createDto.SubjectId,
                StartTime = createDto.StartTime.ToUniversalTime(),
                EndTime = createDto.EndTime.ToUniversalTime(),
                TotalPrice = createDto.TotalPrice,
                Status = SessionStatus.Scheduled,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _sessionRepository.CreateAsync(session);

            // ── Notificar al docente: nueva solicitud ──────────────
            // Persiste en DB + push SignalR en tiempo real
            await _notificationService.NotifySessionBookedAsync(
                teacherId: created.TeacherId,
                studentId: created.StudentId,
                studentName: student.Name,
                sessionId: created.Id,
                subjectName: teacher.UserRoles.FirstOrDefault()?.Role?.Name ?? "la sesión",
                sessionDate: created.StartTime
            );

            return MapToDto(created);
        }

        // ─────────────────────────────────────────────────
        // ACTUALIZAR SESIÓN
        // ─────────────────────────────────────────────────

        public async Task<TutoringSessionDto> UpdateSessionAsync(Guid id, UpdateTutoringSessionDto updateDto)
        {
            var session = await _sessionRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Session {id} not found");

            if (updateDto.StartTime.HasValue || updateDto.EndTime.HasValue)
            {
                if (session.Status != SessionStatus.Scheduled)
                    throw new InvalidOperationException("Only scheduled sessions can be rescheduled");

                var newStart = (updateDto.StartTime ?? session.StartTime).ToUniversalTime();
                var newEnd = (updateDto.EndTime ?? session.EndTime).ToUniversalTime();

                if (newStart <= DateTime.UtcNow)
                    throw new InvalidOperationException("StartTime must be in the future");

                if (newEnd <= newStart)
                    throw new InvalidOperationException("EndTime must be after StartTime");

                session.StartTime = newStart;
                session.EndTime = newEnd;
            }

            SessionStatus? previousStatus = null;

            if (updateDto.Status.HasValue)
            {
                previousStatus = session.Status;
                ValidateStatusTransition(session.Status, updateDto.Status.Value);
                session.Status = updateDto.Status.Value;

                if (updateDto.Status == SessionStatus.Completed && !string.IsNullOrEmpty(session.VideoRoomId))
                    await _hmsService.DisableRoomAsync(session.VideoRoomId);
            }

            var updated = await _sessionRepository.UpdateAsync(session);
            var dto = MapToDto(updated);

            // ── Notificaciones según el nuevo estado ───────────────
            if (previousStatus.HasValue)
            {
                var subjectName = updated.Subject?.Name ?? "la sesión";

                switch (updated.Status)
                {
                    case SessionStatus.Ready:
                    case SessionStatus.Scheduled when previousStatus == SessionStatus.Cancelled:
                        // Docente confirmó (reactivó) → avisar al estudiante
                        await _notificationService.NotifySessionConfirmedAsync(
                            studentId: updated.StudentId,
                            teacherName: updated.Teacher?.Name ?? string.Empty,
                            sessionId: updated.Id,
                            subjectName: subjectName,
                            sessionDate: updated.StartTime
                        );
                        break;

                    case SessionStatus.Cancelled:
                        // Cualquiera canceló — el contexto del llamador sabe quién fue;
                        // aquí usamos el nombre del docente como quien notifica al estudiante.
                        // Si el estudiante canceló, el controlador puede llamar NotifySessionCancelledAsync directamente.
                        await _notificationService.NotifySessionCancelledAsync(
                            recipientId: updated.StudentId,
                            cancelledByName: updated.Teacher?.Name ?? "El docente",
                            sessionId: updated.Id,
                            subjectName: subjectName,
                            sessionDate: updated.StartTime
                        );
                        break;
                }
            }

            return dto;
        }

        public async Task<bool> DeleteSessionAsync(Guid id) =>
            await _sessionRepository.DeleteAsync(id);

        // ─────────────────────────────────────────────────
        // STATS  (sin cambios)
        // ─────────────────────────────────────────────────

        public async Task<SessionStatsDto> GetSessionStatsAsync(Guid? userId = null)
        {
            IEnumerable<TutoringSession> sessions;

            if (userId.HasValue)
            {
                var studentSessions = await _sessionRepository.GetByStudentIdAsync(userId.Value);
                var teacherSessions = await _sessionRepository.GetByTeacherIdAsync(userId.Value);
                sessions = studentSessions.Concat(teacherSessions).DistinctBy(s => s.Id);
            }
            else
            {
                sessions = await _sessionRepository.GetAllAsync();
            }

            var list = sessions.ToList();

            return new SessionStatsDto
            {
                TotalSessions = list.Count,
                ScheduledSessions = list.Count(s => s.Status == SessionStatus.Scheduled),
                ReadySessions = list.Count(s => s.Status == SessionStatus.Ready),
                InProgressSessions = list.Count(s => s.Status == SessionStatus.InProgress),
                CompletedSessions = list.Count(s => s.Status == SessionStatus.Completed),
                CancelledSessions = list.Count(s => s.Status == SessionStatus.Cancelled),
                NoShowSessions = list.Count(s => s.Status == SessionStatus.NoShow)
            };
        }

        // ─────────────────────────────────────────────────
        // VIDEO — 100ms  (sin cambios salvo el _notifications → _notificationService)
        // ─────────────────────────────────────────────────

        public async Task<VideoRoomAccessDto> GetOrCreateVideoRoomAsync(Guid sessionId, Guid requestingUserId)
        {
            var session = await _sessionRepository.GetByIdAsync(sessionId)
                ?? throw new KeyNotFoundException($"Session {sessionId} not found");

            if (session.StudentId != requestingUserId && session.TeacherId != requestingUserId)
                throw new UnauthorizedAccessException("Not a participant of this session");

            if (session.Status == SessionStatus.Cancelled || session.Status == SessionStatus.Completed)
                throw new InvalidOperationException($"Session is {session.Status}");

            if (string.IsNullOrEmpty(session.VideoRoomId))
            {
                var roomName = $"{session.Student?.Name} & {session.Teacher?.Name}";
                session.VideoRoomId = await _hmsService.CreateRoomAsync(sessionId, roomName);
                session.Status = SessionStatus.Ready;
            }

            bool isTeacher = session.TeacherId == requestingUserId;
            var role = isTeacher ? "teacher" : "student";
            var userName = isTeacher
                ? (session.Teacher?.Name ?? requestingUserId.ToString())
                : (session.Student?.Name ?? requestingUserId.ToString());

            string token;
            if (isTeacher)
            {
                session.TeacherToken ??= await _hmsService.GenerateTokenAsync(
                    session.VideoRoomId, requestingUserId.ToString(), role, userName);
                token = session.TeacherToken;
            }
            else
            {
                session.StudentToken ??= await _hmsService.GenerateTokenAsync(
                    session.VideoRoomId, requestingUserId.ToString(), role, userName);
                token = session.StudentToken;
            }

            await _sessionRepository.UpdateAsync(session);

            // Avisar al otro participante que la sala está lista
            var otherId = isTeacher ? session.StudentId : session.TeacherId;
            var otherName = isTeacher ? (session.Teacher?.Name ?? "") : (session.Student?.Name ?? "");
            var subjectName = session.Subject?.Name ?? "la sesión";

            await _notificationService.NotifySessionConfirmedAsync(
                studentId: otherId,
                teacherName: otherName,
                sessionId: session.Id,
                subjectName: subjectName,
                sessionDate: session.StartTime
            );

            return new VideoRoomAccessDto
            {
                RoomId = session.VideoRoomId,
                Token = token,
                RoomUrl = $"https://localhost:4200/classroom/{session.Id}"
            };
        }

        // ─────────────────────────────────────────────────
        // RATING  (sin cambios salvo el _notifications → _notificationService)
        // ─────────────────────────────────────────────────

        public async Task<SessionRatingDto> CreateRatingAsync(CreateSessionRatingDto dto, Guid ratedByUserId)
        {
            var session = await _sessionRepository.GetByIdAsync(dto.SessionId)
                ?? throw new KeyNotFoundException("Session not found");

            if (session.Status != SessionStatus.Completed)
                throw new InvalidOperationException("Can only rate completed sessions");

            if (session.StudentId != ratedByUserId)
                throw new UnauthorizedAccessException("Only the student can rate the session");

            if (session.RatingId is not null)
                throw new InvalidOperationException("Session already has a rating");

            var rating = new SessionRating
            {
                Id = Guid.NewGuid(),
                SessionId = dto.SessionId,
                RatedByUserId = ratedByUserId,
                Score = dto.Score,
                Comment = dto.Comment,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _sessionRepository.CreateRatingAsync(rating);
            session.RatingId = created.Id;
            await _sessionRepository.UpdateAsync(session);

            // Notificar al docente que fue calificado
            // Reutilizamos NotifyPaymentReceivedAsync no aplica aquí —
            // se puede añadir NotifySessionRatedAsync a INotificationService si se necesita
            // Por ahora: notificación genérica directa
            //await _notificationService.CreateAsync(CreateNotificationDto
            //{
            //    UserId = session.TeacherId,
            //    Title = "Nueva calificación ⭐",
            //    Message = $"{session.Student?.Name ?? "Un estudiante"} calificó la sesión con {dto.Score}/5.",
            //    Type = LinaTask.Domain.Models.NotificationType.Info,
            //    Category = LinaTask.Domain.Models.NotificationCategory.General,
            //    ReferenceId = dto.SessionId,
            //    ReferenceType = "Session",
            //    ActionUrl = "/teacher/sessions"
            //});

            return MapRatingToDto(created);
        }

        // ─────────────────────────────────────────────────
        // HELPERS PRIVADOS  (sin cambios)
        // ─────────────────────────────────────────────────

        private static void ValidateStatusTransition(SessionStatus current, SessionStatus next)
        {
            var allowed = new Dictionary<SessionStatus, SessionStatus[]>
            {
                { SessionStatus.Scheduled,  [SessionStatus.Ready, SessionStatus.Cancelled, SessionStatus.NoShow] },
                { SessionStatus.Ready,      [SessionStatus.InProgress, SessionStatus.Cancelled, SessionStatus.NoShow] },
                { SessionStatus.InProgress, [SessionStatus.Completed, SessionStatus.NoShow] },
                { SessionStatus.Completed,  [] },
                { SessionStatus.Cancelled,  [SessionStatus.Scheduled] },
                { SessionStatus.NoShow,     [] }
            };

            if (!allowed.TryGetValue(current, out var validNext) || !validNext.Contains(next))
                throw new InvalidOperationException($"Invalid transition from {current} to {next}");
        }

        private static TutoringSessionDto MapToDto(TutoringSession s) => new()
        {
            Id = s.Id,
            StudentId = s.StudentId,
            StudentName = s.Student?.Name ?? string.Empty,
            TeacherId = s.TeacherId,
            TeacherName = s.Teacher?.Name ?? string.Empty,
            SubjectId = s.SubjectId,
            SubjectName = s.Subject?.Name,
            StartTime = s.StartTime,
            EndTime = s.EndTime,
            Status = s.Status,
            VideoRoomId = s.VideoRoomId,
            RatingId = s.RatingId,
            Rating = s.Rating is null ? null : MapRatingToDto(s.Rating),
            TotalPrice = s.TotalPrice,
            CreatedAt = s.CreatedAt,
            UpdatedAt = s.UpdatedAt
        };

        private static SessionRatingDto MapRatingToDto(SessionRating r) => new()
        {
            Id = r.Id,
            SessionId = r.SessionId,
            RatedByUserId = r.RatedByUserId,
            Score = r.Score,
            Comment = r.Comment,
            CreatedAt = r.CreatedAt
        };
    }
}