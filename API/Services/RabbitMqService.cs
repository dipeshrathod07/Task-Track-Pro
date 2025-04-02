using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace API.Services
{
    public class RabbitMqService : IDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;

        public RabbitMqService(IConnectionFactory factory)
        {
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.ExchangeDeclare("chat_exchange", ExchangeType.Direct);
            _channel.QueueDeclare("chat_messages", durable: true, exclusive: false, autoDelete: false);
            _channel.QueueBind("chat_messages", "chat_exchange", "chat_messages");
        }

        public async Task PublishMessage<T>(string routingKey, T message)
        {
            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.MessageId = Guid.NewGuid().ToString();

            _channel.BasicPublish(
                exchange: "chat_exchange",
                routingKey: routingKey,
                basicProperties: properties,
                body: body);

            await Task.CompletedTask;
        }

        public async Task EnsureQueueExists(string queueName)
        {
            // Declare the queue (will create if it doesn't exist)
            _channel.QueueDeclare(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);
            
            await Task.CompletedTask;
        }

        public void AcknowledgeMessages(string userId)
        {
            try
            {
                var queueName = $"chat_messages_{userId}";
                
                // Declare queue if it doesn't exist
                _channel.QueueDeclare(queueName, 
                    durable: true, 
                    exclusive: false, 
                    autoDelete: false,
                    arguments: null);
                _channel.QueueBind(queueName, "chat_exchange", queueName);
                
                _channel.QueuePurge(queueName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error acknowledging messages: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
        }
    }
}
