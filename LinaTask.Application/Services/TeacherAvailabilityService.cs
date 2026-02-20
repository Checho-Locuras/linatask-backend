using LinaTask.Application.DTOs;
using LinaTask.Application.Services.Interfaces;
using LinaTask.Domain.Enums;
using LinaTask.Domain.Interfaces;
using LinaTask.Domain.Models;

namespace LinaTask.Application.Services
{
    public class TeacherAvailabilityService : ITeacherAvailabilityService
    {
        private readonly ITeacherAvailabilityRepository _repo;
        private readonly ITutoringSessionRepository _sessionRepo;

        private static readonly string[] DayNames =
            { "Domingo", "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado" };

        public TeacherAvailabilityService(
            ITeacherAvailabilityRepository repo,
            ITutoringSessionRepository sessionRepo)
        {
            _repo = repo;
            _sessionRepo = sessionRepo;
        }

        // ── CRUD básico ───────────────────────────────────────

        public async Task<IEnumerable<TeacherAvailabilityDto>> GetByTeacherIdAsync(Guid teacherId)
        {
            var entities = await _repo.GetByTeacherIdAsync(teacherId);
            return entities.Select(MapToDto);
        }

        public async Task<TeacherAvailabilityDto?> GetByIdAsync(Guid id)
        {
            var entity = await _repo.GetByIdAsync(id);
            return entity == null ? null : MapToDto(entity);
        }

        public async Task<TeacherAvailabilityDto> CreateAsync(Guid teacherId, CreateAvailabilityDto dto)
        {
            ValidateTimes(dto.StartTime, dto.EndTime);

            var entity = new TeacherAvailability
            {
                TeacherId = teacherId,
                DayOfWeek = dto.DayOfWeek,
                StartTime = TimeSpan.Parse(dto.StartTime),
                EndTime = TimeSpan.Parse(dto.EndTime),
                SlotDurationMinutes = dto.SlotDurationMinutes,
                IsActive = dto.IsActive,
                Notes = dto.Notes
            };

            var created = await _repo.CreateAsync(entity);
            return MapToDto(created);
        }

        public async Task<TeacherAvailabilityDto> UpdateAsync(Guid id, UpdateAvailabilityDto dto)
        {
            ValidateTimes(dto.StartTime, dto.EndTime);

            var entity = await _repo.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Disponibilidad {id} no encontrada");

            entity.StartTime = TimeSpan.Parse(dto.StartTime);
            entity.EndTime = TimeSpan.Parse(dto.EndTime);
            entity.SlotDurationMinutes = dto.SlotDurationMinutes;
            entity.IsActive = dto.IsActive;
            entity.Notes = dto.Notes;

            var updated = await _repo.UpdateAsync(entity);
            return MapToDto(updated);
        }

        public async Task<bool> DeleteAsync(Guid id) => await _repo.DeleteAsync(id);

        public async Task<IEnumerable<TeacherAvailabilityDto>> BulkSaveAsync(BulkSaveAvailabilityDto dto)
        {
            foreach (var dayGroup in dto.Availabilities.GroupBy(a => a.DayOfWeek))
            {
                var blocks = dayGroup
                    .Select(a => new { Start = TimeSpan.Parse(a.StartTime), End = TimeSpan.Parse(a.EndTime) })
                    .OrderBy(b => b.Start)
                    .ToList();

                for (int i = 0; i < blocks.Count - 1; i++)
                {
                    if (blocks[i].End > blocks[i + 1].Start)
                        throw new ArgumentException($"Los bloques de {dayGroup.Key} se están cruzando.");
                }
            }

            // Borrar todos los registros existentes del docente
            await _repo.DeleteByTeacherIdAsync(dto.TeacherId);

            if (!dto.Availabilities.Any())
                return Enumerable.Empty<TeacherAvailabilityDto>();

            var entities = dto.Availabilities.Select(a =>
            {
                ValidateTimes(a.StartTime, a.EndTime);
                return new TeacherAvailability
                {
                    Id = Guid.NewGuid(),
                    TeacherId = dto.TeacherId,
                    DayOfWeek = a.DayOfWeek,
                    StartTime = TimeSpan.Parse(a.StartTime),
                    EndTime = TimeSpan.Parse(a.EndTime),
                    SlotDurationMinutes = a.SlotDurationMinutes,
                    IsActive = a.IsActive,
                    Notes = a.Notes,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
            }).ToList();

            await _repo.BulkInsertAsync(entities);

            // Recargar con navegación para obtener TeacherName
            var saved = await _repo.GetByTeacherIdAsync(dto.TeacherId);
            return saved.Select(MapToDto);
        }

        // ── Vista semanal con cruce de sesiones ───────────────

        public async Task<TeacherWeeklyAvailabilityDto> GetWeeklyAvailabilityAsync(Guid teacherId, DateTime weekStart)
        {
            // Normalizar al lunes de la semana
            var monday = weekStart.Date;
            while (monday.DayOfWeek != DayOfWeek.Monday)
                monday = monday.AddDays(-1);
            var sunday = monday.AddDays(6);

            // Bloques de disponibilidad del docente
            var blocks = (await _repo.GetByTeacherIdAsync(teacherId))
                .Where(b => b.IsActive)
                .ToList();

            // Sesiones del docente en esa semana
            // Ahora filtramos por StartTime en lugar de SessionDate,
            // y comparamos contra el enum en lugar del string "Cancelled"
            var sessions = (await _sessionRepo.GetByTeacherIdAsync(teacherId))
                .Where(s => s.StartTime.Date >= monday
                         && s.StartTime.Date <= sunday
                         && s.Status != SessionStatus.Cancelled)
                .ToList();

            var result = new TeacherWeeklyAvailabilityDto
            {
                TeacherId = teacherId,
                TeacherName = blocks.FirstOrDefault()?.Teacher?.Name ?? "",
                WeekStart = monday.ToString("yyyy-MM-dd"),
                WeekEnd = sunday.ToString("yyyy-MM-dd")
            };

            // Construir los 7 días
            for (int d = 0; d < 7; d++)
            {
                var currentDate = monday.AddDays(d);
                var dow = (int)currentDate.DayOfWeek;

                var dayDto = new DayAvailabilityDto
                {
                    DayOfWeek = dow,
                    DayName = DayNames[dow],
                    Date = currentDate.ToString("yyyy-MM-dd"),
                    Slots = new List<AvailabilitySlotDto>()
                };

                var dayBlocks = blocks.Where(b => b.DayOfWeek == dow);

                foreach (var block in dayBlocks)
                {
                    var cursor = block.StartTime;

                    while (cursor + TimeSpan.FromMinutes(block.SlotDurationMinutes) <= block.EndTime)
                    {
                        var slotStart = cursor;
                        var slotEnd = cursor + TimeSpan.FromMinutes(block.SlotDurationMinutes);

                        // Colisión: la sesión solapa con el slot si su StartTime < slotEnd
                        // y su EndTime > slotStart — ahora usamos el EndTime real de la sesión
                        var collision = sessions.FirstOrDefault(s =>
                            s.StartTime.Date == currentDate.Date
                            && s.StartTime.TimeOfDay < slotEnd
                            && s.EndTime.TimeOfDay > slotStart);

                        dayDto.Slots.Add(new AvailabilitySlotDto
                        {
                            StartTime = slotStart.ToString(@"hh\:mm"),
                            EndTime = slotEnd.ToString(@"hh\:mm"),
                            IsAvailable = collision == null,
                            BlockedReason = collision != null ? "Sesión agendada" : null
                        });

                        cursor = slotEnd;
                    }
                }

                result.Days.Add(dayDto);
            }

            return result;
        }

        // ── Helpers ───────────────────────────────────────────

        private static TeacherAvailabilityDto MapToDto(TeacherAvailability e) => new()
        {
            Id = e.Id,
            TeacherId = e.TeacherId,
            TeacherName = e.Teacher?.Name ?? "",
            DayOfWeek = e.DayOfWeek,
            DayName = DayNames[e.DayOfWeek],
            StartTime = e.StartTime.ToString(@"hh\:mm"),
            EndTime = e.EndTime.ToString(@"hh\:mm"),
            SlotDurationMinutes = e.SlotDurationMinutes,
            IsActive = e.IsActive,
            Notes = e.Notes,
            CreatedAt = e.CreatedAt
        };

        private static void ValidateTimes(string start, string end)
        {
            if (!TimeSpan.TryParse(start, out var s) || !TimeSpan.TryParse(end, out var e))
                throw new ArgumentException("Formato de hora inválido. Use HH:mm");
            if (s >= e)
                throw new ArgumentException("La hora de inicio debe ser anterior a la hora de fin");
        }
    }
}