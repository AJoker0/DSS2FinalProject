using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using RabbitMQ.Client;

namespace FinalProjectDSS.Controllers
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

        // GET /api/integrations/redis/health
        [HttpGet("redis/health")]
        public async Task<IActionResult> RedisHealth()
        {
            try
            {
                await _cache.SetStringAsync("health_check", "ok");
                var val = await _cache.GetStringAsync("health_check");

                // Строго по спецификации: слово "connected"
                if (val == "ok") return Ok(new { status = "connected", service = "redis" });
                return StatusCode(503, new { status = "error", service = "redis" });
            }
            catch (Exception ex)
            {
                return StatusCode(503, new { status = "error", service = "redis", error = ex.Message });
            }
        }

        // GET /api/integrations/rabbitmq/health
        [HttpGet("rabbitmq/health")]
        public IActionResult RabbitMqHealth()
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _configuration["RabbitHost"] ?? "localhost",
                    UserName = _configuration["RabbitUser"] ?? "guest",
                    Password = _configuration["RabbitPass"] ?? "guest"
                };
                using var connection = factory.CreateConnection();

                // Строго по спецификации: слово "connected"
                if (connection.IsOpen) return Ok(new { status = "connected", service = "rabbitmq" });
                return StatusCode(503, new { status = "error", service = "rabbitmq" });
            }
            catch (Exception ex)
            {
                return StatusCode(503, new { status = "error", service = "rabbitmq", error = ex.Message });
            }
        }
    }
}