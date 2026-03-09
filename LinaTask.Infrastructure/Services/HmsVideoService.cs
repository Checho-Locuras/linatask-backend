using LinaTask.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace LinaTask.Infrastructure.Services
{
    /// <summary>
    /// Integración con la API de 100ms (https://www.100ms.live).
    /// El AppSecret se usa como UTF-8 (texto plano, no decodificado desde Base64).
    /// El Issuer es un valor interno de 100ms, diferente al AppAccessKey.
    /// </summary>
    public class HmsVideoService : IHmsVideoService
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _config;
        private readonly ILogger<HmsVideoService> _logger;

        private string AppAccessKey => _config["Hms:AppAccessKey"]!;
        private string AppSecret => _config["Hms:AppSecret"]!;
        private string BaseUrl => _config["Hms:BaseUrl"] ?? "https://api.100ms.live/v2";
        private string TemplateId => _config["Hms:TemplateId"]!;
        private string Issuer => _config["Hms:Issuer"]!;

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
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", GenerateManagementToken());

            var payload = new
            {
                name = $"session-{sessionId}",
                description = sessionName,
                template_id = TemplateId,
                region = "us"
            };

            var response = await _http.PostAsync(
                $"{BaseUrl}/rooms",
                new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            );

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("100ms CreateRoom failed ({Status}): {Body}", response.StatusCode, error);
                throw new HttpRequestException($"100ms {response.StatusCode}: {error}");
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var roomId = doc.RootElement.GetProperty("id").GetString()!;

            _logger.LogInformation("100ms room created: {RoomId} for session {SessionId}", roomId, sessionId);
            return roomId;
        }

        // ─────────────────────────────────────────────────
        // GENERAR TOKEN DE PARTICIPANTE
        // ─────────────────────────────────────────────────

        public Task<string> GenerateTokenAsync(
            string roomId, string userId, string role, string userName)
        {
            var now = DateTimeOffset.UtcNow;

            var payloadDict = new JwtPayload
            {
                { "version",    2            },
                { "type",       "app"        },
                { "app_data",   null         },
                { "role",       role         },
                { "room_id",    roomId       },
                { "user_id",    userId       },
                { "user_name",  userName     },
                { "iss",        Issuer       },
                { "sub",        "api"        },
                { "nbf",        now.ToUnixTimeSeconds()             },
                { "iat",        now.ToUnixTimeSeconds()             },
                { "exp",        now.AddHours(4).ToUnixTimeSeconds() },
                { "jti",        Guid.NewGuid().ToString("N")        }
            };

            payloadDict.Add("access_key", AppAccessKey);

            return Task.FromResult(BuildJwt(payloadDict));
        }

        // ─────────────────────────────────────────────────
        // DESHABILITAR ROOM
        // ─────────────────────────────────────────────────

        public async Task DisableRoomAsync(string roomId)
        {
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", GenerateManagementToken());

            var payload = new { enabled = false };
            var response = await _http.PostAsync(
                $"{BaseUrl}/rooms/{roomId}",
                new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            );

            if (!response.IsSuccessStatusCode)
                _logger.LogWarning("100ms DisableRoom failed ({Status}) for room {RoomId}",
                    response.StatusCode, roomId);
        }

        // ─────────────────────────────────────────────────
        // HELPERS PRIVADOS
        // ─────────────────────────────────────────────────

        private string GenerateManagementToken()
        {
            var now = DateTimeOffset.UtcNow;

            var payloadDict = new JwtPayload
            {
                { "access_key", AppAccessKey                        },
                { "type",       "management"                        },
                { "version",    2                                   },
                { "nbf",        now.ToUnixTimeSeconds()             },
                { "iat",        now.ToUnixTimeSeconds()             },
                { "exp",        now.AddHours(24).ToUnixTimeSeconds() },
                { "jti",        Guid.NewGuid().ToString("N")        }
            };

            return BuildJwt(payloadDict);
        }

        /// <summary>
        /// Firma el JWT con AppSecret en UTF-8 (no Base64).
        /// Sin "kid" en el header — 100ms no lo espera.
        /// </summary>
        private string BuildJwt(JwtPayload payload)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(AppSecret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var header = new JwtHeader(creds);

            return new JwtSecurityTokenHandler()
                .WriteToken(new JwtSecurityToken(header, payload));
        }
    }
}