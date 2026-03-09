using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using LinaTask.Application.Services.Interfaces;
using LinaTask.Domain.DTOs.Chat;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace LinaTask.Aplication.Services
{
    public class AzureBlobFileUploadService : IFileUploadService
    {
        private readonly BlobContainerClient _containerClient;

        public AzureBlobFileUploadService(IConfiguration configuration)
        {
            var connectionString = configuration["AzureBlob:ConnectionString"];
            var containerName = configuration["AzureBlob:ContainerName"];

            _containerClient = new BlobContainerClient(connectionString, containerName);
            _containerClient.CreateIfNotExists(PublicAccessType.Blob);
        }

        public async Task<FileUploadResult> UploadAsync(IFormFile file, string folder = "uploads")
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("Archivo inválido");

            var uniqueFileName = $"{folder}/{Guid.NewGuid()}_{file.FileName}";
            var blobClient = _containerClient.GetBlobClient(uniqueFileName);

            using var stream = file.OpenReadStream();
            await blobClient.UploadAsync(stream, new BlobHttpHeaders
            {
                ContentType = file.ContentType
            });

            return new FileUploadResult
            {
                Url = blobClient.Uri.ToString(),
                FileName = uniqueFileName,
                FileSize = file.Length,
                ContentType = file.ContentType
            };
        }

        public async Task<bool> DeleteAsync(string fileUrl)
        {
            var blobName = GetBlobNameFromUrl(fileUrl);
            var blobClient = _containerClient.GetBlobClient(blobName);

            var response = await blobClient.DeleteIfExistsAsync();
            return response.Value;
        }

        public string GetPublicUrl(string fileName, string folder = "uploads")
        {
            var blobClient = _containerClient.GetBlobClient($"{folder}/{fileName}");
            return blobClient.Uri.ToString();
        }

        private string GetBlobNameFromUrl(string fileUrl)
        {
            var uri = new Uri(fileUrl);
            return uri.AbsolutePath.TrimStart('/');
        }
    }
}
