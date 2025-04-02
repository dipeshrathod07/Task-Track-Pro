using Microsoft.AspNetCore.Mvc;
using Repositories.Interfaces;
using Repositories.Models;
using Microsoft.AspNetCore.SignalR;

namespace API.Controllers
{
    [Route("api/notification")]
    [ApiController]
    public class NotificationApiController : ControllerBase
    {
        private readonly INotificationInterface _notification;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationApiController(INotificationInterface notification, IHubContext<NotificationHub> hubContext)
        {
            _notification = notification;
            _hubContext = hubContext;
        }


        [HttpGet("{userId}")]
        public async Task<IActionResult> GetAll(Guid userId)
        {
            var notifications = await _notification.GetAllByUserId(userId);
            if (notifications == null)
            {
                return StatusCode(500, new { message = "Failed to get notifications" });
            }
            return Ok(new { data = notifications });
        }


        [HttpGet("unread/{userId}")]
        public async Task<IActionResult> GetAllUnread(Guid userId)
        {
            var notifications = await _notification.GetAllUnreadByUserId(userId);
            if (notifications == null)
            {
                return StatusCode(500, new { message = "Failed to get notifications" });
            }
            return Ok(new { data = notifications });
        }


        [HttpPut("mark-read/{notificationId}")]
        public async Task<IActionResult> MarkAsRead(int notificationId)
        {
            var result = await _notification.MarkAsRead(notificationId);
            if (!result)
                return NotFound(new { message = "Notification not found" });

            return Ok(new { message = "Notification marked as read" });
        }


        [HttpPut("mark-all-read/{userId}")]
        public async Task<IActionResult> MarkAllAsRead(Guid userId)
        {
            var result = await _notification.MarkAllAsRead(userId);
            if (result == false)
            {
                return StatusCode(500, new { message = "Failed to mark all notifications as read" });
            }
            return Ok(new { message = "All notifications marked as read" });
        }


        [HttpPost]
        public async Task<IActionResult> Add([FromBody] Notification notification)
        {
            var notificationId = await _notification.Add(notification);
            
            if (notificationId <= 0)
            {
                return StatusCode(500, new { message = "Failed to create notification" });
            }
            await _hubContext.Clients.All.SendAsync("ReceiveNotifications", notification);
            return Ok(new
            {
                message = "Notification created successfully",
                notificationId
            });
        }

        [HttpPost("send-notification/{userId}")]
        public async Task<IActionResult> SendNotification(string userId, [FromBody] Notification notification)
        {
            System.Console.WriteLine("IN notification api :: " + userId);
            await _hubContext.Clients.All.SendAsync("ReceiveNotifications", notification);
            return Ok(new { message = "Notification sent!" });
        }

    }
}   