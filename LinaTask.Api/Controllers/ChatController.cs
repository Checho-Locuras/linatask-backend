using LinaTask.Api.Common;
using LinaTask.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinaTask.Api.Controllers
{
    // =====================================================
    // ChatController.cs
    // =====================================================
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ChatController : BaseController
    {
        private readonly IChatService _chatService;
        private readonly IFileUploadService _fileService;

        public ChatController(IChatService chatService, IFileUploadService fileService)
        {
            _chatService = chatService;
            _fileService = fileService;
        }

        // GET api/chat/conversations
        [HttpGet("conversations")]
        public async Task<IActionResult> GetConversations()
        {
            var userId = CurrentUserId;
            var conversations = await _chatService.GetConversationsAsync(userId);
            return Ok(conversations);
        }

        // POST api/chat/conversations
        // Body: { "otherUserId": "uuid" }
        [HttpPost("conversations")]
        public async Task<IActionResult> GetOrCreateConversation([FromBody] Guid otherUserId)
        {
            var userId = CurrentUserId;
            var conversation = await _chatService.GetOrCreateConversationAsync(userId, otherUserId);
            return Ok(conversation);
        }

        // GET api/chat/conversations/{id}/messages?page=1&pageSize=50
        [HttpGet("conversations/{conversationId:guid}/messages")]
        public async Task<IActionResult> GetMessages(
            Guid conversationId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var userId = CurrentUserId;
            var messages = await _chatService.GetMessagesAsync(conversationId, userId, page, pageSize);
            return Ok(messages);
        }

        // POST api/chat/upload
        // Subir archivo antes de enviar el mensaje
        [HttpPost("upload")]
        [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No se recibió ningún archivo");

            var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp",
                                   "application/pdf", "text/plain",
                                   "application/msword",
                                   "application/vnd.openxmlformats-officedocument.wordprocessingml.document" };

            if (!allowedTypes.Contains(file.ContentType))
                return BadRequest("Tipo de archivo no permitido");

            var result = await _fileService.UploadAsync(file, "chat");
            return Ok(new { url = result.Url, fileName = file.FileName, fileSize = file.Length });
        }

        // PATCH api/chat/conversations/{id}/read
        [HttpPatch("conversations/{conversationId:guid}/read")]
        public async Task<IActionResult> MarkAsRead(Guid conversationId)
        {
            var userId = CurrentUserId;
            await _chatService.MarkMessagesAsReadAsync(conversationId, userId);
            return NoContent();
        }
    }
}
