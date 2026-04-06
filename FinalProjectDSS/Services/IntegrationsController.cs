using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using RabbitMQ.Client;



namespace FinalProjectDSS.Services
{
    [ApiController]
    [Route("api/[controller]")]
    public class IntegrationsController : ControllerBase
    {
        private readonly IDistributedCache _cache;
        private readonly IConfiguration _configuration;

        public IntegrationsController(IDistributedCache cache, IConfiguration configuration)
        {
            _cache = cache;
            _configuration = configuration;
        }

        // Get /api/integrations/redis/health
        [HttpGet("redis/health")]
        public async Task<IActionResult> RedisHealth()
        {
            try
            {
                // Trying to write down and read the test value
                await _cache.SetStringAsync("health_check", "ok");
                var val = await _cache.GetStringAsync("heath_check");

                if (val == "ok") return Ok(new { status = "healthy", service = "redis" });
                return StatusCode(503, new { status = "unhealthy", service = "redis" });
            }
            catch
            {
                return StatusCode(503, new { status = "unhealthy", service = "redis" });
            }
        }
        // GET / api /integrations/rabbitmq/health
        [HttpGet("rebbitmq/health")]
        public IActionResult RabbitMQHealth()
        {
            try
            {
                // try to connect to RabbitMQ
                var factory = new ConnectionFactory { HostName = _configuration["RabbitHost"] ?? "localhost" };
                using var connection = factory.CreateConnection();

                if (connection.IsOpen) return Ok(new { status = "healthy", service = "rabbitmq" });
                return StatusCode(503, new { status = "unhealthy", service = "rabbitmq" });

            }
            catch
            {
                return StatusCode(503, new { status = "unhealthy", serice = "rabbitmq" });
            }
        }
    }
    
}
