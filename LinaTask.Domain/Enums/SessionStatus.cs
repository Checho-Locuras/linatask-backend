// LinaTask.Domain/Enums/SessionStatus.cs

namespace LinaTask.Domain.Enums
{
    /// <summary>
    /// Ciclo de vida de una sesión de tutoría.
    /// </summary>
    public enum SessionStatus
    {
        Pending = 0,      // NUEVO → esperando aceptación del docente
        Scheduled = 1,    // Aceptada oficialmente
        Ready = 2,
        InProgress = 3,
        Completed = 4,
        Cancelled = 5,
        Rejected = 6,     // NUEVO → docente rechazó
        NoShow = 7
    }
}