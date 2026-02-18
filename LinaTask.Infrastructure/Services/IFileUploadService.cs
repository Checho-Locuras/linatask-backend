using LinaTask.Domain.DTOs.Chat;
using Microsoft.AspNetCore.Http;

namespace LinaTask.Application.Services.Interfaces
{
    public interface IFileUploadService
    {
        /// <summary>
        /// Sube un archivo y retorna la URL pública
        /// </summary>
        Task<FileUploadResult> UploadAsync(IFormFile file, string folder = "uploads");

        /// <summary>
        /// Elimina un archivo usando su URL o path
        /// </summary>
        Task<bool> DeleteAsync(string fileUrl);

        /// <summary>
        /// Obtiene la URL pública de un archivo
        /// </summary>
        string GetPublicUrl(string fileName, string folder = "uploads");
    }
}
