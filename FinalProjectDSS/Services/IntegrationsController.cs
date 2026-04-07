using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using RabbitMQ.Client;

namespace FinalProjectDSS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class IntegrationsController : ControllerBase
    {
        private readonly IDistributedCache _cache; // Redis cache service
        private readonly IConfiguration _configuration; // App configuration (for RabbitMQ settings)

        public IntegrationsController(IDistributedCache cache, IConfiguration configuration)
        {
            _cache = cache;
            _configuration = configuration;
        }

        // health check endpoint for Redis: GET /api/integrations/redis/health
        [HttpGet("redis/health")]
        public async Task<IActionResult> RedisHealth()
        {
            try
            {
                // try to set and get a value from Redis to check connectivity
                await _cache.SetStringAsync("health_check", "ok");
                var val = await _cache.GetStringAsync("health_check");

                // If successful, return "connected" status
                if (val == "ok") return Ok(new { status = "connected", service = "redis" });
                return StatusCode(503, new { status = "error", service = "redis" });
            }
            catch (Exception ex)
            {
                return StatusCode(503, new { status = "error", service = "redis", error = ex.Message });
            }
        }

        // Health check endpoint for RabbitMQ: GET /api/integrations/rabbitmq/health
        [HttpGet("rabbitmq/health")]
        public IActionResult RabbitMqHealth()
        {
            try
            {
                // Create a RabbitMQ connection using settings from configuration
                var factory = new ConnectionFactory
                {
                    HostName = _configuration["RabbitHost"] ?? "localhost",
                    UserName = _configuration["RabbitUser"] ?? "guest",
                    Password = _configuration["RabbitPass"] ?? "guest"
                };
                using var connection = factory.CreateConnection();

                // If connection is open, return "connected" status
                if (connection.IsOpen) return Ok(new { status = "connected", service = "rabbitmq" });
                return StatusCode(503, new { status = "error", service = "rabbitmq" });
            }
            catch (Exception ex)
            {
                // If any error occurs, return error status and message
                return StatusCode(503, new { status = "error", service = "rabbitmq", error = ex.Message });
            }
        }
    }
}