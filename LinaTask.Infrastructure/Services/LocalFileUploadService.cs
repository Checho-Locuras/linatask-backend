using LinaTask.Application.Services.Interfaces;
using LinaTask.Domain.DTOs.Chat;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;

namespace LinaTask.Application.Services
{
    public class LocalFileUploadService : IFileUploadService
    {
        private readonly IWebHostEnvironment _env;

        public LocalFileUploadService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task<FileUploadResult> UploadAsync(IFormFile file, string folder = "uploads")
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("Archivo inválido");

            var uploadsRoot = Path.Combine(_env.WebRootPath, folder);

            if (!Directory.Exists(uploadsRoot))
                Directory.CreateDirectory(uploadsRoot);

            var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
            var fullPath = Path.Combine(uploadsRoot, uniqueFileName);

            using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream);

            return new FileUploadResult
            {
                Url = $"/{folder}/{uniqueFileName}",
                FileName = uniqueFileName,
                FileSize = file.Length,
                ContentType = file.ContentType
            };
        }

        public async Task<bool> DeleteAsync(string fileUrl)
        {
            if (string.IsNullOrWhiteSpace(fileUrl))
                return false;

            var fileName = Path.GetFileName(fileUrl);
            var fullPath = Path.Combine(_env.WebRootPath, "uploads", fileName);

            if (!File.Exists(fullPath))
                return false;

            await Task.Run(() => File.Delete(fullPath));
            return true;
        }

        public string GetPublicUrl(string fileName, string folder = "uploads")
        {
            return $"/{folder}/{fileName}";
        }
    }
}
