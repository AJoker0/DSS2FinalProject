using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FinalProjectDSS.Data;
using FinalProjectDSS.Models;
using FinalProjectDSS.DTOs;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.EntityFrameworkCore;

namespace FinalProjectDSS.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class TodosController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TodosController(AppDbContext context)
        {
            _context = context;
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

        // --- НОВЫЙ МЕТОД: ПУБЛИЧНЫЕ ЗАДАЧИ (БЕЗ АВТОРИЗАЦИИ) ---
        [AllowAnonymous] // Разрешаем вход без токена
        [HttpGet("public")]
        public async Task<IActionResult> GetPublicTodos([FromQuery] string? status, [FromQuery] string? priority,
            [FromQuery] string? dueFrom, [FromQuery] string? dueTo, [FromQuery] string? search,
            [FromQuery] string sortBy = "createdAt", [FromQuery] string sortDir = "desc",
            [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var query = _context.Todos.Where(t => t.IsPublic); // Только публичные
            return await ApplyFiltersAndReturn(query, page, pageSize, status, priority, dueFrom, dueTo, search, sortBy, sortDir);
        }

        // --- ОБНОВЛЕННЫЙ МЕТОД: ЛИЧНЫЕ ЗАДАЧИ ---
        [HttpGet]
        public async Task<IActionResult> GetAllTodos([FromQuery] string? status, [FromQuery] string? priority,
            [FromQuery] string? dueFrom, [FromQuery] string? dueTo, [FromQuery] string? search,
            [FromQuery] string sortBy = "createdAt", [FromQuery] string sortDir = "desc",
            [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var userId = GetUserId();
            var query = _context.Todos.Where(t => t.UserId == userId); // Только свои
            return await ApplyFiltersAndReturn(query, page, pageSize, status, priority, dueFrom, dueTo, search, sortBy, sortDir);
        }

        // Общая логика фильтрации для обоих методов
        private async Task<IActionResult> ApplyFiltersAndReturn(IQueryable<TodoItem> query, int page, int pageSize,
            string? status, string? priority, string? dueFrom, string? dueTo, string? search, string sortBy, string sortDir)
        {
            // 1. Валидация пагинации (ТЗ 5.5)
            if (page < 1 || pageSize < 1 || pageSize > 50) return BadRequest();

            // 2. Фильтрация по статусу
            if (status == "active") query = query.Where(t => !t.IsCompleted);
            else if (status == "completed") query = query.Where(t => t.IsCompleted);

            // 3. Фильтрация по приоритету
            if (!string.IsNullOrEmpty(priority) && Enum.TryParse<Priority>(priority, out var p))
                query = query.Where(t => t.Priority == p);

            // 4. Фильтрация по датам
            if (!string.IsNullOrEmpty(dueFrom) && DateTime.TryParse(dueFrom, out var dFrom))
                query = query.Where(t => t.DueDate >= dFrom.ToUniversalTime());
            if (!string.IsNullOrEmpty(dueTo) && DateTime.TryParse(dueTo, out var dTo))
                query = query.Where(t => t.DueDate <= dTo.ToUniversalTime());

            // 5. Поиск (search)
            if (!string.IsNullOrEmpty(search))
                query = query.Where(t => t.Title.Contains(search) || (t.Details != null && t.Details.Contains(search)));

            // 6. Сортировка
            query = sortBy.ToLower() switch
            {
                "duedate" => sortDir == "asc" ? query.OrderBy(t => t.DueDate) : query.OrderByDescending(t => t.DueDate),
                "priority" => sortDir == "asc" ? query.OrderBy(t => t.Priority) : query.OrderByDescending(t => t.Priority),
                "title" => sortDir == "asc" ? query.OrderBy(t => t.Title) : query.OrderByDescending(t => t.Title),
                _ => sortDir == "asc" ? query.OrderBy(t => t.CreatedAt) : query.OrderByDescending(t => t.CreatedAt)
            };

            // 7. Считаем итоги для пагинации
            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // 8. Применяем пагинацию (Skip и Take)
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

        // Остальные методы CRUD (Create, GetById, Update, Delete) остаются без изменений...
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
                Priority = Enum.Parse<Priority>(request.Priority),
                DueDate = request.DueDate != null ? DateTime.Parse(request.DueDate).ToUniversalTime() : null,
                IsPublic = request.IsPublic,
                IsCompleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Todos.Add(todo);
            _context.SaveChanges();
            return Created($"/api/todos/{todo.Id}", MapToResponse(todo));
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
            todo.Priority = Enum.Parse<Priority>(request.Priority);
            todo.DueDate = request.DueDate != null ? DateTime.Parse(request.DueDate).ToUniversalTime() : null;
            todo.IsPublic = request.IsPublic;
            todo.IsCompleted = request.IsCompleted;
            todo.UpdatedAt = DateTime.UtcNow;
            _context.SaveChanges();
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
            return NoContent();
        }
    }
}