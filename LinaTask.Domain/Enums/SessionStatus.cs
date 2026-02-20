// LinaTask.Domain/Enums/SessionStatus.cs

namespace LinaTask.Domain.Enums
{
    /// <summary>
    /// Ciclo de vida de una sesión de tutoría.
    /// </summary>
    public enum SessionStatus
    {
        /// <summary>Reservada pero sin confirmar sala de video.</summary>
        Scheduled = 0,

        /// <summary>Sala de video creada; ambas partes pueden unirse.</summary>
        Ready = 1,

        /// <summary>Al menos un participante entró a la sala.</summary>
        InProgress = 2,

        /// <summary>Sesión finalizada correctamente.</summary>
        Completed = 3,

        /// <summary>Cancelada antes de comenzar.</summary>
        Cancelled = 4,

        /// <summary>Uno o ambos participantes no se presentaron.</summary>
        NoShow = 5
    }
}