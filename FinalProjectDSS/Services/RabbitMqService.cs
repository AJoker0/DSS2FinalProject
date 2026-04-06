using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace FinalProjectDSS.Services
{
    public class RabbitMqService
    {
        private readonly string _hostName;
        private readonly string _userName;
        private readonly string _password;

        public RabbitMqService(IConfiguration configuration)
        {
            _hostName = configuration["RabbitHost"] ?? "localhost";
            _userName = configuration["RabbitUser"] ?? "guest";
            _password = configuration["RabbitPass"] ?? "guest";
        }

        public void PublishEvent(string eventType, object data)
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _hostName,
                    UserName = _userName,
                    Password = _password
                };
                using var connection = factory.CreateConnection();
                using var channel = connection.CreateModel();

                channel.ExchangeDeclare(exchange: "todo_events", type: ExchangeType.Fanout);

                var message = new { Event = eventType, Data = data, Timestamp = DateTime.UtcNow };
                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

                channel.BasicPublish(exchange: "todo_events", routingKey: "", basicProperties: null, body: body);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"RabbitMQ Error: {ex.Message}");
            }
        }
    }
}