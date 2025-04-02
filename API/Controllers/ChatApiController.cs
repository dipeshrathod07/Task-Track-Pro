using API.Services;
using Microsoft.AspNetCore.Mvc;
using Repositories.Interfaces;
using Repositories.Models;

namespace API.Controllers
{
    [Route("api/chat")]
    [ApiController]
    public class ChatApiController : ControllerBase
    {
        private readonly IChatInterface _chat;
        private readonly RabbitMqService _rabbitMqService;

        public ChatApiController(IChatInterface chat, RabbitMqService rabbitMqService)
        {
            _chat = chat;
            _rabbitMqService = rabbitMqService;
        }


        #region SaveChat
        [HttpPost]
        public async Task<IActionResult> SaveChat([FromForm] Chat chat)
        {
            var chatId = await _chat.SaveChat(chat);
            if (chatId > 0)
            {
                // Ensure queue exists and publish message to RabbitMQ with delivery status
                await _rabbitMqService.EnsureQueueExists("chat_messages");
                await _rabbitMqService.PublishMessage("chat_messages", new
                {
                    chatId,
                    chat.Message,
                    chat.SenderId,
                    chat.ReceiverId,
                    Timestamp = DateTime.UtcNow,
                    Status = "sent"
                });
                
                return Ok(new { chatId, status = "sent" });
            }
            
            return BadRequest(new { message = "Failed to save chat" });
        }
        #endregion


        #region GetChatHistory
        [HttpGet("history/{senderId}/{receiverId}")]
        public async Task<IActionResult> GetChatHistory(Guid senderId, Guid receiverId)
        {
            var chats = await _chat.GetChatHistory(senderId, receiverId);
            if (chats != null)
            {
                // Acknowledge messages for this user
                _rabbitMqService.AcknowledgeMessages(receiverId.ToString());
                return Ok(new { data = chats });
            }
            
            return NotFound(new { message = "No chat history found" });
        }
        #endregion


        #region MarkChatAsRead
        [HttpGet("unread/{userId}")]
        public async Task<IActionResult> GetUnreadChats(Guid userId)
        {
            var chats = await _chat.GetUnreadChats(userId);
            if (chats != null)
                return Ok(new { data = chats });
            
            return NotFound(new { message = "No unread messages" });
        }
        #endregion


        #region MarkChatAsRead
        [HttpPut("mark-read/{chatId}")]
        public async Task<IActionResult> MarkAsRead(int chatId)
        {
            var result = await _chat.MarkChatAsRead(chatId);
            if (result > 0)
                return Ok(new { message = "Message marked as read" });
            
            return NotFound(new { message = "Message not found" });
        }
        #endregion


        #region MarkAllAsRead
        [HttpPut("mark-all-read")]
        public async Task<IActionResult> MarkAllAsRead([FromQuery] Guid senderId, [FromQuery] Guid receiverId)
        {
            var result = await _chat.MarkAllChatsAsRead(senderId, receiverId);
            if (result)
                return Ok(new { message = "All messages marked as read" });
            
            return BadRequest(new { message = "Failed to mark messages as read" });
        }
        #endregion

    }
}