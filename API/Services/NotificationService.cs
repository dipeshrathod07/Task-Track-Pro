using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Repositories.Interfaces;
using Repositories.Models;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace API.Services
{
    public class NotificationBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly IHubContext<ChatHub> _chatHubContext;
        private readonly IConnectionMultiplexer _redis;
        private readonly TimeSpan _interval = TimeSpan.FromSeconds(10);

        public NotificationBackgroundService(IServiceScopeFactory scopeFactory, IHubContext<NotificationHub> hubContext, IHubContext<ChatHub> chatHubContext, IConnectionMultiplexer redis)
        {
            _scopeFactory = scopeFactory;
            _hubContext = hubContext;
            _chatHubContext = chatHubContext;
            _redis = redis;
        }

        protected override async System.Threading.Tasks.Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("✅ Notification & Chat Background Service Started");

            while (!stoppingToken.IsCancellationRequested)
            {

                try
                {
                    var db = _redis.GetDatabase();
                    var onlineUsers = (await db.HashGetAllAsync("UserStatus")).Where(u => u.Value == "online").Select(u => u.Name.ToString()).ToList();

                     if (onlineUsers.Any())
                    {
                        Console.WriteLine("📌 Current Online Users: " + string.Join(", ", onlineUsers));
                        await _chatHubContext.Clients.All.SendAsync("ReceiveOnlineUsers", onlineUsers);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error in NotificationBackgroundService: {ex.Message}");
                }  
                try
                {
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var notificationRepo = scope.ServiceProvider.GetRequiredService<INotificationInterface>();
                        var chatRepo = scope.ServiceProvider.GetRequiredService<IChatInterface>();

                        // 🔹 Fetch all unread notifications from the database
                        List<Notification> notifications = await notificationRepo.GetAllUnreadNotifications();
                        // 🔹 Fetch all unread chat messages from the database
                        List<Chat> chats = await chatRepo.GetUnreadChatsAll();

                        // 🔥 Send Unread Notifications via SignalR
                        if (notifications.Any())
                        {
                            Console.WriteLine($"🔔 {notifications.Count} unread notifications found!");

                            var sortedNoti = notifications.OrderBy(n => n.CreatedAt);

                            // Group notifications by UserId and send them
                            var groupedNotifications = sortedNoti.GroupBy(n => n.UserId);
                            foreach (var group in groupedNotifications)
                            {
                                string userId = group.Key.ToString();
                                var userNotifications = group.ToList();

                                Console.WriteLine($"📢 Sending {userNotifications.Count} notifications to User {userId}");

                                // 🔹 Send notifications only to the specific user
                                await _hubContext.Clients.All.SendAsync("ReceiveNotifications", userNotifications);
                            }
                        }
                        else
                        {
                            Console.WriteLine("⏳ No new unread notifications...");
                        }

                        // 🔥 Send Unread Chat Messages via SignalR
                        if (chats.Any())
                        {
                            Console.WriteLine($"💬 {chats.Count} unread chat messages found!");

                            // Group messages by ReceiverId (UserId)
                            var groupedChats = chats.GroupBy(c => c.ReceiverId);
                            foreach (var group in groupedChats)
                            {
                                string receiverId = group.Key.ToString();
                                var userChats = group.ToList();

                                Console.WriteLine($"📨 Sending {userChats.Count} messages to User {receiverId}");

                                // 🔹 Send chat messages only to the intended recipient
                                await _chatHubContext.Clients.All.SendAsync("ReceiveMessages", userChats);
                            }
                        }
                        else
                        {
                            Console.WriteLine("⏳ No new unread messages...");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error in NotificationChatBackgroundService: {ex.Message}");
                }

                // 🔹 Wait before fetching again (polling interval)
                await System.Threading.Tasks.Task.Delay(_interval, stoppingToken);
            }

            Console.WriteLine("⚠ Notification & Chat Background Service Stopped");
        }
    }
}
