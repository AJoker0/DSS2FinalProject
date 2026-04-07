using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace FinalProjectDSS.Services
{
    // Service for publishing events to RabbitMQ
    public class RabbitMqService
    {
        // RabbitMQ connection settings
        private readonly string _hostName;
        private readonly string _userName;
        private readonly string _password;

        // Constructor: reads RabbitMQ settings from configuration
        public RabbitMqService(IConfiguration configuration)
        {
            _hostName = configuration["RabbitHost"] ?? "localhost";
            _userName = configuration["RabbitUser"] ?? "guest";
            _password = configuration["RabbitPass"] ?? "guest";
        }

        // Publishes an event to the "todo_events" exchange in RabbitMQ
        public void PublishEvent(string eventType, object data)
        {
            try
            {
                // Create a connection to RabbitMQ
                var factory = new ConnectionFactory
                {
                    HostName = _hostName,
                    UserName = _userName,
                    Password = _password
                };
                using var connection = factory.CreateConnection();
                using var channel = connection.CreateModel();

                // Declare a fanout exchange for todo events
                channel.ExchangeDeclare(exchange: "todo_events", type: ExchangeType.Fanout);

                // Create the event message with type, data, and timestamp
                var message = new { Event = eventType, Data = data, Timestamp = DateTime.UtcNow };
                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

                // Publish the message to the exchange
                channel.BasicPublish(exchange: "todo_events", routingKey: "", basicProperties: null, body: body);
            }
            catch (Exception ex)
            {
                // Log any errors to the console
                Console.WriteLine($"RabbitMQ Error: {ex.Message}");
            }
        }
    }
}