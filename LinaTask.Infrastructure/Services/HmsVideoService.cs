using LinaTask.Application.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace LinaTask.Infrastructure.Services
{
    /// <summary>
    /// Integración con la API de 100ms (https://www.100ms.live).
    /// Docs: https://www.100ms.live/docs/server-side/v2/introduction/request-signing
    /// </summary>
    public class HmsVideoService : IHmsVideoService
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _config;
        private readonly ILogger<HmsVideoService> _logger;

        // Claves obtenidas del dashboard de 100ms
        private string AppAccessKey => _config["Hms:AppAccessKey"]!;
        private string AppSecret => _config["Hms:AppSecret"]!;
        private string BaseUrl => _config["Hms:BaseUrl"] ?? "https://api.100ms.live/v2";

        /// <summary>
        /// Template ID del "classroom" configurado en el dashboard de 100ms.
        /// Puedes tener uno para 1-a-1 y otro para grupos.
        /// </summary>
        private string TemplateId => _config["Hms:TemplateId"]!;

        public HmsVideoService(HttpClient http, IConfiguration config, ILogger<HmsVideoService> logger)
        {
            _http = http;
            _config = config;
            _logger = logger;
        }

        // ─────────────────────────────────────────────────
        // CREAR ROOM
        // ─────────────────────────────────────────────────

        public async Task<string> CreateRoomAsync(Guid sessionId, string sessionName)
        {
            var managementToken = GenerateManagementToken();

            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", managementToken);

            var payload = new
            {
                name = $"session-{sessionId}",           // nombre único
                description = sessionName,
                template_id = TemplateId,
                region = "us"                              // o "eu", "in" según tu audiencia
            };

            var response = await _http.PostAsync(
                $"{BaseUrl}/rooms",
                new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            );

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var roomId = doc.RootElement.GetProperty("id").GetString()!;

            _logger.LogInformation("100ms room created: {RoomId} for session {SessionId}", roomId, sessionId);
            return roomId;
        }

        // ─────────────────────────────────────────────────
        // GENERAR TOKEN DE PARTICIPANTE
        // ─────────────────────────────────────────────────

        /// <param name="role">Debe coincidir con un rol definido en el template de 100ms
        ///   (ej: "teacher", "student", "viewer").</param>
        public async Task<string> GenerateTokenAsync(
            string roomId, string userId, string role, string userName)
        {
            // El token de participante se firma localmente con AppAccessKey + AppSecret
            // Docs: https://www.100ms.live/docs/concepts/v2/concepts/security-and-tokens

            var now = DateTimeOffset.UtcNow;

            var claims = new[]
            {
                new Claim("room_id",    roomId),
                new Claim("user_id",    userId),
                new Claim("role",       role),
                new Claim("user_name",  userName),
                new Claim("type",       "app"),                 // "app" = participante
                new Claim("version",    "2"),
                new Claim("iat",        now.ToUnixTimeSeconds().ToString()),
                new Claim("exp",        now.AddHours(4).ToUnixTimeSeconds().ToString()),
                new Claim("jti",        Guid.NewGuid().ToString("N"))
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(AppSecret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var header = new JwtHeader(creds);
            header["kid"] = AppAccessKey;                       // requerido por 100ms

            var payload = new JwtPayload(claims);
            var token = new JwtSecurityToken(header, payload);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // ─────────────────────────────────────────────────
        // DESHABILITAR ROOM
        // ─────────────────────────────────────────────────

        public async Task DisableRoomAsync(string roomId)
        {
            var managementToken = GenerateManagementToken();
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", managementToken);

            var payload = new { enabled = false };
            var response = await _http.PostAsync(
                $"{BaseUrl}/rooms/{roomId}",
                new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            );

            if (!response.IsSuccessStatusCode)
                _logger.LogWarning("Could not disable 100ms room {RoomId}: {Status}", roomId, response.StatusCode);
        }

        // ─────────────────────────────────────────────────
        // MANAGEMENT TOKEN (server-to-server)
        // ─────────────────────────────────────────────────

        /// <summary>
        /// Token para llamadas server-side a la API de 100ms.
        /// Diferente al token de participante.
        /// </summary>
        private string GenerateManagementToken()
        {
            var now = DateTimeOffset.UtcNow;

            var claims = new[]
            {
                new Claim("access_key", AppAccessKey),
                new Claim("type",       "management"),
                new Claim("version",    "2"),
                new Claim("iat",        now.ToUnixTimeSeconds().ToString()),
                new Claim("exp",        now.AddHours(24).ToUnixTimeSeconds().ToString()),
                new Claim("jti",        Guid.NewGuid().ToString("N"))
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(AppSecret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var header = new JwtHeader(creds);
            header["kid"] = AppAccessKey;

            var payload = new JwtPayload(claims);
            var token = new JwtSecurityToken(header, payload);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}