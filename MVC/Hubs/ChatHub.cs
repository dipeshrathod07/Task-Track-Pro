using Microsoft.AspNetCore.SignalR;

namespace TaskTrackPro.MVC.Hubs
{
    public class ChatHub : Hub
    {
        public async Task SendMessage(string toUserId, string message)
        {
            await Clients.User(toUserId).SendAsync("ReceiveMessage", Context.UserIdentifier, message);
        }
    }
}