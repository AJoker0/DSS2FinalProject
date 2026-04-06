using System;

namespace FinalProjectDSS.Models
{
    public class TodoItem
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; } // External key to the user
        public string Title { get; set; } = string.Empty;
        public string? Details { get; set; } // question mark means that input can be empty (optional)
        public Priority Priority { get; set; }
        public DateTime? DueDate { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsPublic { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // navigation properties for communication with User 
        public User? User { get; set; }
    }
}
