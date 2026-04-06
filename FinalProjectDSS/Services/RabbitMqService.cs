using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace FinalProjectDSS.Services
{
    public class RabbitMqService
    {
        private readonly string _hostName;

        public RabbitMqService(IConfiguration configuration)
        {
            // take host from Docker (rabbitmq) or locally 
            _hostName = configuration["RabbitHost"] ?? "localhost";
        }

        public void PublishEvent(string eventType, object data)
        {
            try
            {
                var factory = new ConnectionFactory { HostName = _hostName };
                using var connection = factory.CreateConnection();
                using var channel = connection.CreateModel();

                // create "changer" 
                channel.ExchangeDeclare(exchange: "todo_events", type: ExchangeType.Fanout);

                //Form beautiful message
                var message = new { Event = eventType, Data = data, Timestamp = DateTime.UtcNow };
                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

                // send to quieue 
                channel.BasicPublish(exchange: "todo_events", routingKey: "", basicProperties: null, body: body);

                
                }
            catch (Exception ex)
            {
                Console.WriteLine($"RabbitMQ Error: {ex.Message}");
            }
            
        }
    }
}
