using Repositories.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Repositories.Interfaces;
using Microsoft.AspNetCore.SignalR;
using System.Text;
using System.Text.Json;

namespace API.Services
{
    public class RabbitMqListener : BackgroundService
    {
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IModel _channel;
        private readonly IConnection _connection;

        public RabbitMqListener(
            IConnectionFactory factory,
            IHubContext<ChatHub> hubContext,
            IServiceScopeFactory scopeFactory)
        {
            _hubContext = hubContext;
            _scopeFactory = scopeFactory;
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // Declare main exchange
            _channel.ExchangeDeclare("chat_exchange", ExchangeType.Direct);
        }

        private void EnsureQueueExists(string userId)
        {
            var queueName = $"chat_messages_{userId}";
            _channel.QueueDeclare(queueName, 
                durable: true, 
                exclusive: false, 
                autoDelete: false,
                arguments: null);
            _channel.QueueBind(queueName, "chat_exchange", queueName);
        }

      protected override async System.Threading.Tasks.Task ExecuteAsync(CancellationToken stoppingToken)
{
    var consumer = new AsyncEventingBasicConsumer(_channel);
    
    consumer.Received += async (model, ea) =>
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var chatInterface = scope.ServiceProvider.GetRequiredService<IChatInterface>();

            var body = ea.Body.ToArray();
            var message = JsonSerializer.Deserialize<Chat>(Encoding.UTF8.GetString(body));

            if (message != null)
            {
                // Ensure queue exists for receiver
                EnsureQueueExists(message.ReceiverId.ToString());
                
                // Store the message in the database if needed
                // await chatInterface.SaveChat(message);
                
                // Forward the message to the specific user via SignalR
                await _hubContext.Clients.User(message.ReceiverId.ToString())
                    .SendAsync("ReceiveMessage", message, cancellationToken: stoppingToken);
                
                // Also notify the sender about delivery status
                await _hubContext.Clients.User(message.SenderId.ToString())
                    .SendAsync("MessageStatus", message.ChatId, "delivered");
            }

            _channel.BasicAck(ea.DeliveryTag, false);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing message: {ex.Message}");
            _channel.BasicNack(ea.DeliveryTag, false, true);
        }
    };

    // Listen on the main chat_messages queue
    _channel.QueueDeclare(
        queue: "chat_messages",
        durable: true,
        exclusive: false,
        autoDelete: false,
        arguments: null);

    _channel.BasicConsume(
        queue: "chat_messages",
        autoAck: false,
        consumer: consumer);

    await System.Threading.Tasks.Task.Delay(Timeout.Infinite, stoppingToken);
}
        public override void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
            base.Dispose();
        }
    }
}
