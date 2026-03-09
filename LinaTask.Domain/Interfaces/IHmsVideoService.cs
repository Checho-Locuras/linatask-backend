namespace LinaTask.Domain.Interfaces
{
    public interface IHmsVideoService
    {
        /// <summary>Crea una room en 100ms y retorna el roomId.</summary>
        Task<string> CreateRoomAsync(Guid sessionId, string sessionName);

        /// <summary>Genera un token de acceso JWT para un participante.</summary>
        Task<string> GenerateTokenAsync(string roomId, string userId, string role, string userName);

        /// <summary>Deshabilita una room cuando la sesión termina.</summary>
        Task DisableRoomAsync(string roomId);
    }
}
