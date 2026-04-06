using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FinalProjectDSS.Data;
using FinalProjectDSS.Models;
using FinalProjectDSS.DTOs;
using System.IdentityModel.Tokens.Jwt;



namespace FinalProjectDSS.Controllers
{
    // [Authorize] means that all endpoints in this controller require authentication (a valid JWT token)
    [Authorize]
    [ApiController]
    [Route("api/[controller]")] // /api/todos
    public class  TodosController : ControllerBase 
    {
        private readonly AppDbContext _context;

        public TodosController(AppDbContext context)
        {
            _context = context;
        }
        // take user id from JWT token
        private Guid GetUserId()
        {
        
            var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                            ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            return Guid.Parse(userIdString!);
        }
        // Turning a model from a DB into a beautiful answer 
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
        [HttpPost]
        public IActionResult CreateTodo([FromBody] CreateTodoRequest request)
        {
            var userId = GetUserId(); // Find out who makes the request

            var todo = new TodoItem
            {
                Id = Guid.NewGuid(),
                UserId = userId, // Task for a specific user
                Title = request.Title,
                Details = request.Details,
                Priority = Enum.Parse<Priority>(request.Priority),// Turning the line into Enum
                DueDate = request.DueDate != null ? DateTime.Parse(request.DueDate).ToUniversalTime() : null,
                IsPublic = request.IsPublic,
                IsCompleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow

            };

            _context.Todos.Add(todo);
            _context.SaveChanges();

            //return status 201 Created
            return Created($"/api/todos/{todo.Id}", MapToResponse(todo));
        }

        [HttpGet]
        public IActionResult GetAllTodos()
        {
            var userId = GetUserId(); // know who asks tasks

            //search in database for all tasks where UserId is the same as our user's ID
            var todos = _context.Todos
                .Where(t => t.UserId == userId)
                .ToList();

            // transfer each database model into a beautiful DTO response
            var response = todos.Select(t => MapToResponse(t));

            // return 200 OK and array of tasks
            return Ok(response);
        }

        [HttpGet("{id}")]
        public IActionResult GetTodo(Guid id)
        {
            var userId = GetUserId();
            var todo = _context.Todos.FirstOrDefault(t => t.Id == id);

            if (todo == null) return NotFound(); //404 if task not exist

            // check Does the task belong to the person who requests it
            if (todo.UserId != userId) return StatusCode(403, new { message = "Forbidden" }); //403


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
        public IActionResult DeleteToDO(Guid id)
        {
            var userId = GetUserId();
            var todo = _context.Todos.FirstOrDefault(t => t.Id == id);

            if (todo == null) return NotFound();
            if (todo.UserId != userId) return StatusCode(403, new { message = "Forbidden" });

            _context.Todos.Remove(todo);
            _context.SaveChanges();

            return NoContent(); // 204 No Content (due successful removal)
        }


    }

    
}
