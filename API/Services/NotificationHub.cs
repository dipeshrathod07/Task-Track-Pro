using Microsoft.AspNetCore.SignalR;
using Repositories.Interfaces;
using Repositories.Models;
using StackExchange.Redis;
using System.Threading.Tasks;

public class NotificationHub : Hub
{
    private readonly INotificationInterface _noti;
    private readonly IConnectionMultiplexer _redis;


    public NotificationHub(INotificationInterface n, IConnectionMultiplexer redis)
    {
        _redis = redis;
        _noti = n;
    }

    // public async System.Threading.Tasks.Task FetchNotifications(string userId)
    // {
    //     System.Console.WriteLine("IN notification hub :: " + userId);
    //     var notifications = await _noti.GetAllUnreadByUserId(Guid.Parse(userId));
    //     await Clients.Caller.SendAsync("ReceiveNotifications", notifications); 
    // }

    public override async System.Threading.Tasks.Task OnConnectedAsync()
    {
        var userId = Context.GetHttpContext()?.Request.Query["userId"].ToString();
        if (!string.IsNullOrEmpty(userId))
        {
            var db = _redis.GetDatabase();
            await db.HashSetAsync("UserConnections", userId, Context.ConnectionId);
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);
            await db.HashSetAsync("UserStatus", userId, "online");

            await Clients.Others.SendAsync("UserOnline", userId);
            Console.WriteLine($"User {userId} connected with Connection ID: {Context.ConnectionId}");
        }
        await base.OnConnectedAsync();
    }

    public override async System.Threading.Tasks.Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.GetHttpContext()?.Request.Query["userId"].ToString();
        if (!string.IsNullOrEmpty(userId))
        {
            var db = _redis.GetDatabase();
            await db.HashDeleteAsync("UserConnections", userId);
            await db.HashSetAsync("UserStatus", userId, "offline");

            await Clients.Others.SendAsync("UserOffline", userId);
            Console.WriteLine($"User {userId} disconnected");
        }
        await base.OnDisconnectedAsync(exception);
    }

    public async System.Threading.Tasks.Task FetchNotifications(string userId)
    {
        List<Notification> notifications = await _noti.GetAllUnreadByUserId(Guid.Parse(userId));
        Console.WriteLine("Fetch :: " + string.Join(", ", notifications.Select(n => n.ToString())));
        // Console.WriteLine("Fetch :: " + notifications[0].Title);
        await Clients.User(userId).SendAsync("ReceiveNotifications", notifications);
    }

    // ✅ Add a method to broadcast notifications in real-time
    public async System.Threading.Tasks.Task SendNotificationToUser(string userId)
    {
        var notifications = await _noti.GetAllUnreadByUserId(Guid.Parse(userId));
        Console.WriteLine("Send :: " + notifications.ToString());
        await Clients.User(userId).SendAsync("ReceiveNotifications", notifications);
    }
}
