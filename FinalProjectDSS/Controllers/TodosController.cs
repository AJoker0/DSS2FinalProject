using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FinalProjectDSS.Data;
using FinalProjectDSS.Models;
using FinalProjectDSS.DTOs;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using FinalProjectDSS.Services;
using System.Text.Json;

namespace FinalProjectDSS.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class TodosController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IDistributedCache _cache;
        private readonly RabbitMqService _rabbitMqService;

        public TodosController(AppDbContext context, IDistributedCache cache, RabbitMqService rabbitMqService)
        {
            _context = context;
            _cache = cache;
            _rabbitMqService = rabbitMqService;
        }

        private Guid GetUserId()
        {
            var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                            ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            return Guid.Parse(userIdString!);
        }

        private TodoResponse MapToResponse(TodoItem todo)
        {
            return new TodoResponse
            {
                Id = todo.Id,
                Title = todo.Title,
                Details = todo.Details,
                Priority = todo.Priority.ToString(),
                DueDate = todo.DueDate?.ToString("yyyy-MM-dd"),
                IsCompleted = todo.IsCompleted,
                IsPublic = todo.IsPublic,
                CreatedAt = todo.CreatedAt,
                UpdatedAt = todo.UpdatedAt
            };
        }

        // --- ПУБЛИЧНЫЕ ЗАДАЧИ ---
        [AllowAnonymous]
        [HttpGet("public")]
        public async Task<IActionResult> GetPublicTodos([FromQuery] string? status, [FromQuery] string? priority,
            [FromQuery] string? dueFrom, [FromQuery] string? dueTo, [FromQuery] string? search,
            [FromQuery] string sortBy = "createdAt", [FromQuery] string sortDir = "desc",
            [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            string cacheKey = $"public_todos_{page}_{pageSize}_{search}_{status}_{priority}_{sortBy}_{sortDir}";

            var cachedData = await _cache.GetStringAsync(cacheKey);
            if (!string.IsNullOrEmpty(cachedData))
            {
                return Content(cachedData, "application/json");
            }

            var query = _context.Todos.Where(t => t.IsPublic);
            var result = await ApplyFiltersAndReturn(query, page, pageSize, status, priority, dueFrom, dueTo, search, sortBy, sortDir) as OkObjectResult;

            if (result != null && result.Value != null)
            {
                // Фикс для фронтенда: сохраняем в кэш с маленькой буквы (camelCase)
                var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(result.Value, jsonOptions), new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1)
                });

                return result;
            }

            return BadRequest();
        }

        // --- ЛИЧНЫЕ ЗАДАЧИ ---
        [HttpGet]
        public async Task<IActionResult> GetAllTodos([FromQuery] string? status, [FromQuery] string? priority,
            [FromQuery] string? dueFrom, [FromQuery] string? dueTo, [FromQuery] string? search,
            [FromQuery] string sortBy = "createdAt", [FromQuery] string sortDir = "desc",
            [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var userId = GetUserId();
            var query = _context.Todos.Where(t => t.UserId == userId);
            return await ApplyFiltersAndReturn(query, page, pageSize, status, priority, dueFrom, dueTo, search, sortBy, sortDir);
        }

        // --- ЛОГИКА ФИЛЬТРАЦИИ С ЖЕСТКИМИ ПРОВЕРКАМИ (Раздел 5.4) ---
        private async Task<IActionResult> ApplyFiltersAndReturn(IQueryable<TodoItem> query, int page, int pageSize,
            string? status, string? priority, string? dueFrom, string? dueTo, string? search, string sortBy, string sortDir)
        {
            // Строгие проверки из спецификации
            if (page < 1 || pageSize < 1 || pageSize > 50) return BadRequest();
            if (!string.IsNullOrEmpty(search) && search.Length > 100) return BadRequest();

            status = status?.ToLower() ?? "all";
            if (status != "all" && status != "active" && status != "completed") return BadRequest();

            sortDir = sortDir?.ToLower() ?? "desc";
            if (sortDir != "asc" && sortDir != "desc") return BadRequest();

            // Фильтры
            if (status == "active") query = query.Where(t => !t.IsCompleted);
            else if (status == "completed") query = query.Where(t => t.IsCompleted);

            if (!string.IsNullOrEmpty(priority) && Enum.TryParse<Priority>(priority, true, out var p))
                query = query.Where(t => t.Priority == p);

            if (!string.IsNullOrEmpty(dueFrom) && DateTime.TryParse(dueFrom, out var dFrom))
                query = query.Where(t => t.DueDate >= dFrom.ToUniversalTime());
            if (!string.IsNullOrEmpty(dueTo) && DateTime.TryParse(dueTo, out var dTo))
                query = query.Where(t => t.DueDate <= dTo.ToUniversalTime());

            if (!string.IsNullOrEmpty(search))
                query = query.Where(t => t.Title.Contains(search) || (t.Details != null && t.Details.Contains(search)));

            // Сортировка
            query = sortBy.ToLower() switch
            {
                "duedate" => sortDir == "asc" ? query.OrderBy(t => t.DueDate).ThenBy(t => t.Id) : query.OrderByDescending(t => t.DueDate).ThenBy(t => t.Id),
                "priority" => sortDir == "asc" ? query.OrderBy(t => t.Priority).ThenBy(t => t.Id) : query.OrderByDescending(t => t.Priority).ThenBy(t => t.Id),
                "title" => sortDir == "asc" ? query.OrderBy(t => t.Title).ThenBy(t => t.Id) : query.OrderByDescending(t => t.Title).ThenBy(t => t.Id),
                _ => sortDir == "asc" ? query.OrderBy(t => t.CreatedAt).ThenBy(t => t.Id) : query.OrderByDescending(t => t.CreatedAt).ThenBy(t => t.Id)
            };

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            var response = new PagedResponse<TodoResponse>
            {
                Items = items.Select(MapToResponse).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages
            };

            return Ok(response);
        }

        // --- CRUD ОПЕРАЦИИ ---
        [HttpPost]
        public IActionResult CreateTodo([FromBody] CreateTodoRequest request)
        {
            var userId = GetUserId();
            var todo = new TodoItem
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = request.Title,
                Details = request.Details,
                Priority = Enum.Parse<Priority>(request.Priority, true),
                DueDate = string.IsNullOrWhiteSpace(request.DueDate) ? null : DateTime.Parse(request.DueDate).ToUniversalTime(),
                IsPublic = request.IsPublic,
                IsCompleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Todos.Add(todo);
            _context.SaveChanges();

            _rabbitMqService.PublishEvent("TodoCreated", new { Id = todo.Id, Title = todo.Title, IsPublic = todo.IsPublic });

            return CreatedAtAction(nameof(GetTodo), new { id = todo.Id }, MapToResponse(todo));
        }

        [HttpGet("{id}")]
        public IActionResult GetTodo(Guid id)
        {
            var userId = GetUserId();
            var todo = _context.Todos.FirstOrDefault(t => t.Id == id);

            if (todo == null) return NotFound();
            if (todo.UserId != userId) return StatusCode(403, new { message = "Forbidden" });

            return Ok(MapToResponse(todo));
        }

        [HttpPut("{id}")]
        public IActionResult UpdateTodo(Guid id, [FromBody] UpdateTodoRequest request)
        {
            var userId = GetUserId();
            var todo = _context.Todos.FirstOrDefault(t => t.Id == id);

            if (todo == null) return NotFound();
            if (todo.UserId != userId) return StatusCode(403, new { message = "Forbidden" });

            todo.Title = request.Title;
            todo.Details = request.Details;
            todo.Priority = Enum.Parse<Priority>(request.Priority, true);
            todo.DueDate = string.IsNullOrWhiteSpace(request.DueDate) ? null : DateTime.Parse(request.DueDate).ToUniversalTime();
            todo.IsPublic = request.IsPublic;
            todo.IsCompleted = request.IsCompleted;
            todo.UpdatedAt = DateTime.UtcNow;

            _context.SaveChanges();

            _rabbitMqService.PublishEvent("TodoUpdated", new { Id = todo.Id, Title = todo.Title });

            return Ok(MapToResponse(todo));
        }

        [HttpPatch("{id}/completion")]
        public IActionResult SetCompletion(Guid id, [FromBody] SetCompletionRequest request)
        {
            var userId = GetUserId();
            var todo = _context.Todos.FirstOrDefault(t => t.Id == id);

            if (todo == null) return NotFound();
            if (todo.UserId != userId) return StatusCode(403, new { message = "Forbidden" });

            todo.IsCompleted = request.IsCompleted;
            todo.UpdatedAt = DateTime.UtcNow;

            _context.SaveChanges();

            // ДОБАВЛЕНО ТРЕБОВАНИЕ ИЗ ТЗ (Раздел 9: Отправка TodoCompleted)
            _rabbitMqService.PublishEvent("TodoCompleted", new { Id = todo.Id, IsCompleted = todo.IsCompleted });

            return Ok(MapToResponse(todo));
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteTodo(Guid id)
        {
            var userId = GetUserId();
            var todo = _context.Todos.FirstOrDefault(t => t.Id == id);

            if (todo == null) return NotFound();
            if (todo.UserId != userId) return StatusCode(403, new { message = "Forbidden" });

            _context.Todos.Remove(todo);
            _context.SaveChanges();

            _rabbitMqService.PublishEvent("TodoDeleted", new { Id = id });

            return NoContent();
        }
    }
}