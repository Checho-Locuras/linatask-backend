using LinaTask.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace LinaTask.Application.Services.Interfaces
{
    public interface ITeacherAvailabilityService
    {
        Task<IEnumerable<TeacherAvailabilityDto>> GetByTeacherIdAsync(Guid teacherId);
        Task<TeacherAvailabilityDto?> GetByIdAsync(Guid id);
        Task<TeacherAvailabilityDto> CreateAsync(Guid teacherId, CreateAvailabilityDto dto);
        Task<TeacherAvailabilityDto> UpdateAsync(Guid id, UpdateAvailabilityDto dto);
        Task<bool> DeleteAsync(Guid id);

        /// <summary>
        /// Reemplaza TODA la disponibilidad del docente en una operación atómica.
        /// Ideal para el guardado del calendario.
        /// </summary>
        Task<IEnumerable<TeacherAvailabilityDto>> BulkSaveAsync(BulkSaveAvailabilityDto dto);

        /// <summary>
        /// Devuelve la disponibilidad semanal del docente para una semana concreta,
        /// cruzando los bloques con las sesiones ya agendadas.
        /// </summary>
        Task<TeacherWeeklyAvailabilityDto> GetWeeklyAvailabilityAsync(Guid teacherId, DateTime weekStart);
    }
}
